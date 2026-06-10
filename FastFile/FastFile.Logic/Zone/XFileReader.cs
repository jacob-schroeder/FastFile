using FastFile.Logic.Extensions;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public class XFileReader
{
    private readonly ReadOnlyMemory<byte> _memory;
    private ReadOnlySpan<byte> Span => _memory.Span;
    private int _position = 0;
    
    private XFile _header;
    private XBlock[] _streamBlocks = []; //g_streamBlocks
    private readonly Stack<XBlock> _blockStack = new();
    private XBlock _activeBlock = null;
    
    private XAssetList? _assetList;
    
    public XFileReader(byte[] buffer, Action<int, int>? assetReadProgress = null)
    {
        _memory = buffer.AsMemory();
    }

    public XFileReader Read()
    {
        Load_Header();
        Load_XAssetList();
        
        byte[] a = _streamBlocks[0].WrittenSpan.ToArray();
        byte[] b = _streamBlocks[4].WrittenSpan.ToArray();

        File.WriteAllBytes("/Users/jacob/Downloads/streamsnew/temp.block", a);
        File.WriteAllBytes("/Users/jacob/Downloads/streamsnew/large.block", b);
        
        return this;
    }

    private void Load_Header()
    {
        const int blockCount = (int)XFILE_BLOCK.MAX_XFILE_COUNT;
        const int tempBlock = (int)XFILE_BLOCK.TEMP;
        
        _header = new XFile
        {
            Size = Span.ReadInt32(ref _position),
            ExternalSize = Span.ReadInt32(ref _position),
            BlockSize = new int[blockCount]
        };
        
        _streamBlocks = new XBlock[blockCount];
        for (int i = 0; i < blockCount; i++)
        {
            _header.BlockSize[i] = Span.ReadInt32(ref _position);
            _streamBlocks[i] = new XBlock((XFILE_BLOCK)i, _header.BlockSize[i]);
        }
        
        _activeBlock = _streamBlocks[tempBlock];
    }
    
    private void Load_XAssetList()
    {
        _assetList = new XAssetList
        {
            ScriptStringCount = ReadInt32(),
            ScriptStringsPtr = ReadPointer<XPointer<string?>[]>(),
            AssetCount = ReadInt32(),
            AssetsPtr = ReadPointer<XAsset[]>(),
        };
 
        MaterializeScriptStrings(_assetList);
        //MaterializeAssets(_assetList);
    }
    
    private void MaterializeScriptStrings(XAssetList list)
    {
        var ptr = list.ScriptStringsPtr;

        if (!TryMaterializePointer(
                ptr,
                () => new XBlockAddress(
                    XFILE_BLOCK.LARGE,
                    _streamBlocks[(int)XFILE_BLOCK.LARGE].Position)))
        {
            ptr.Value = [];
            return;
        }

        WithStreamBlock(ptr.Address!.Value.Block, () =>
        {
            SeekOrVerify(ptr.Address.Value.Offset);

            var stringPointers = new XPointer<string?>[list.ScriptStringCount];

            for (int i = 0; i < stringPointers.Length; i++)
                stringPointers[i] = ReadPointer<string?>();

            for (int i = 0; i < stringPointers.Length; i++)
                MaterializeCStringPointer(stringPointers[i]);

            ptr.Value = stringPointers;
        });
    }
    
    private void MaterializeCStringPointer(XPointer<string?> ptr)
    {
        if (!TryMaterializePointer(ptr,() => _activeBlock.Address ))
        {
            ptr.Value = null;
            return;
        }

        WithStreamBlock(ptr.Address!.Value.Block, () =>
        {
            SeekOrVerify(ptr.Address.Value.Offset);
            ptr.Value = ReadCString();
        });
    }
    
    private int ReadInt32()
    {
        int value = Span.ReadInt32(ref _position);
        _activeBlock.WriteInt32(value);

        return value;
    }

    private string ReadCString()
    {
        string value = Span.ReadCStringAt(ref _position);
        _activeBlock.WriteCString(value);
        
        return value;
    }

    #region PointerLogic
    private void SeekOrVerify(int expectedOffset)
    {
        int currentOffset = _activeBlock.Position;

        if (currentOffset != expectedOffset)
        {
            throw new InvalidDataException(
                $"Expected {_activeBlock.BlockType} offset 0x{expectedOffset:X}, " +
                $"but current offset is 0x{currentOffset:X}.");
        }
    }
    
    private bool TryMaterializePointer<T>(
        XPointer<T> ptr,
        Func<XBlockAddress> inlineAddressFactory)
    {
        switch (ptr.Kind)
        {
            case PointerKind.Null:
                return false;

            case PointerKind.Offset:
                ptr.Address = DecodeXPointer(ptr.Raw);
                break;

            case PointerKind.Inline:
                ptr.Address = inlineAddressFactory();
                break;

            case PointerKind.Insert:
                throw new NotSupportedException($"Insert pointer not implemented for {typeof(T).Name}.");

            default:
                throw new InvalidDataException($"Unknown pointer kind {ptr.Kind}.");
        }

        PatchPointer(ptr);
        return true;
    }
    
    private XBlockAddress DecodeXPointer(int raw)
    {
        uint value = unchecked((uint)raw);

        var block = (XFILE_BLOCK)(value >> 28);
        int offset = (int)(value & 0x0FFFFFFF) - 1;

        return new XBlockAddress(block, offset);
    }
    
    private static int EncodeBlockAddress(XBlockAddress address)
    {
        return ((int)address.Block << 28) | (address.Offset + 1);
    }
    
    private XPointer<T> ReadPointer<T>()
    {
        int raw = Span.ReadInt32(ref _position);

        int patchOffset = _activeBlock.Position;

        // placeholder for patch.
        _activeBlock.WriteInt32(raw);

        return new XPointer<T>
        {
            Raw = raw,
            PatchAddress = new XBlockAddress(_activeBlock.BlockType, patchOffset),
            Kind = raw switch
            {
                0  => PointerKind.Null,
                -1 => PointerKind.Inline,
                -2 => PointerKind.Insert,
                _  => PointerKind.Offset
            }
        };
    }
    
    private void PatchPointer<T>(XPointer<T> ptr)
    {
        if (ptr.Address is null)
            return;

        int encoded = EncodeBlockAddress(ptr.Address.Value);

        _streamBlocks[(int)ptr.PatchAddress!.Value.Block]
            .PatchInt32(ptr.PatchAddress.Value.Offset, encoded);
    }
    #endregion
    
    #region BlockLogic
    private void WithStreamBlock(XFILE_BLOCK block, Action action)
    {
        PushStreamBlock(block);

        try
        {
            action();
        }
        finally
        {
            PopStreamBlock();
        }
    }
    
    private void PushStreamBlock(XFILE_BLOCK block)
    {
        _blockStack.Push(_activeBlock);
        _activeBlock = _streamBlocks[(int)block];
    }

    private void PopStreamBlock()
    {
        _activeBlock = _blockStack.Pop();
    }
    #endregion

    #region Exposed
    public XFile GetHeader()
    {
        return _header;
    }

    public XAssetList GetAssetList()
    {
        return _assetList;
    }
    #endregion
}