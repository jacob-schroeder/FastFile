using FastFile.Logic.Extensions;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Localize;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Logic.Zone;

public partial class XFileReader
{
    private readonly ReadOnlyMemory<byte> _memory;
    private ReadOnlySpan<byte> Span => _memory.Span;
    private int _position;

    private XFile _header = null!;
    private XBlock[] _streamBlocks = [];
    private readonly Stack<XBlock> _blockStack = new();
    private XBlock _activeBlock = null!;

    private XAssetList? _assetList;

    public XFileReader(byte[] buffer, Action<int, int>? assetReadProgress = null)
    {
        _memory = buffer.AsMemory();
    }
    
    public XFileReader DumpBlocks()
    {
        const int blockCount = (int)XFILE_BLOCK.MAX_XFILE_COUNT;

        for (int i = 0; i < blockCount; i++)
        {
            string path = $"/Users/jacob/Downloads/streamsnew/{(XFILE_BLOCK)i}.block";
            var block = _streamBlocks[i].WrittenSpan.ToArray();
            
            File.WriteAllBytes(path, block);
        }

        return this;
    }

    public XFileReader Read()
    {
        Load_Header();
        Load_XAssetList();

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
            int blockSize = Span.ReadInt32(ref _position);

            if (blockSize < 0)
                throw new InvalidDataException($"Invalid negative block size {blockSize} for block {(XFILE_BLOCK)i}.");

            _header.BlockSize[i] = blockSize;
            _streamBlocks[i] = new XBlock((XFILE_BLOCK)i, blockSize);
        }

        _activeBlock = _streamBlocks[tempBlock];
    }

    private void Load_XAssetList()
    {
        _assetList = new XAssetList
        {
            ScriptStringCount = ReadInt32(),
            ScriptStringsPtr = ReadDirectPointer<XPointer<string?>[]>(),
            AssetCount = ReadInt32(),
            AssetsPtr = ReadDirectPointer<XAsset[]>(),
        };

        MaterializeScriptStrings(_assetList);
        MaterializeAssets(_assetList);
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
                stringPointers[i] = ReadDirectPointer<string?>();

            for (int i = 0; i < stringPointers.Length; i++)
                MaterializeCStringPointer(stringPointers[i]);

            ptr.Value = stringPointers;
        });
    }

    private void MaterializeAssets(XAssetList list)
    {
        var ptr = list.AssetsPtr;

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

            var assets = new XAsset[list.AssetCount];

            // Phase 1: read the whole XAsset table.
            // This advances _position past all type+pointer rows.
            for (int i = 0; i < assets.Length; i++)
            {
                assets[i] = new XAsset
                {
                    Type = (XAssetType)ReadInt32(),

                    // Asset header pointer exists in the zone,
                    // but is not emitted into large.block.
                    XAssetPtr = ReadAliasPointer<BaseAsset>()
                };
            }

            // Phase 2: now _position points at the first asset payload.
            for (int i = 0; i < assets.Length; i++)
                MaterializeAsset(assets[i]);

            ptr.Value = assets;
        });
    }

    private void MaterializeAsset(XAsset asset)
    {
        var ptr = asset.XAssetPtr;

        if (ptr.Kind == PointerKind.Null)
        {
            ptr.Value = null;
            return;
        }

        ptr.Value = LoadAssetByType(asset.Type, ptr);
    }
    
    private BaseAsset LoadAssetByType(XAssetType type, XPointer<BaseAsset> ptr)
    {
        return type switch
        {
            XAssetType.Localize => LoadAssetRoot<LocalizeEntry>(ptr),
            XAssetType.RawFile => LoadAssetRoot<RawFile>(ptr),
            XAssetType.MenuFile => LoadAssetRoot<MenuList>(ptr),
            _ => throw new NotImplementedException(type.ToString())
        };
    }
    
    private T LoadAssetRoot<T>(XPointer<BaseAsset> assetPtr)
        where T : BaseAsset, new()
    {
        TryMaterializePointer(
            assetPtr,
            () => new XBlockAddress(XFILE_BLOCK.TEMP, _streamBlocks[(int)XFILE_BLOCK.TEMP].Position));

        return WithStreamBlock(assetPtr.Address!.Value.Block, () =>
        {
            SeekOrVerify(assetPtr.Address.Value.Offset);

            var obj = new T();

            ReadObjectFields(obj);
            ResolveObjectPointers(obj);

            return obj;
        });
    }

    private void MaterializeCStringPointer(XPointer<string?> ptr)
    {
        if (!TryMaterializePointer(ptr, () => _activeBlock.Address))
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

    #region Assets

    private MaterialTechniqueSet LoadTechset(XPointer<BaseAsset> ptr)
    {
        throw new NotImplementedException();
    }

    private LocalizeEntry LoadLocalize(XPointer<BaseAsset> ptr)
    {
        if (!TryMaterializePointer(
                ptr,
                () => new XBlockAddress(
                    XFILE_BLOCK.TEMP,
                    _streamBlocks[(int)XFILE_BLOCK.TEMP].Position)))
        {
            throw new InvalidDataException("Localize asset pointer was null.");
        }

        return WithStreamBlock(ptr.Address!.Value.Block, () =>
        {
            SeekOrVerify(ptr.Address.Value.Offset);

            var valuePtr = ReadDirectPointer<string?>();
            var namePtr = ReadDirectPointer<string?>();

            WithStreamBlock(XFILE_BLOCK.LARGE, () =>
            {
                MaterializeCStringPointer(valuePtr);
                MaterializeCStringPointer(namePtr);
            });

            return new LocalizeEntry
            {
                ValuePtr = valuePtr,
                NamePtr = namePtr
            };
        });
    }

    #endregion

    #region Exposed

    public XFile GetHeader()
    {
        return _header;
    }

    public XAssetList GetAssetList()
    {
        return _assetList
            ?? throw new InvalidOperationException("XAssetList has not been loaded yet.");
    }

    #endregion
}