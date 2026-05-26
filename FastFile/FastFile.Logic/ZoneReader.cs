using FastFile.Logic.Assets;
using FastFile.Logic.Extensions;
using FastFile.Models.Assets;
using FastFile.Models.Zone;
using FastFile.Models.Data;

namespace FastFile.Logic;

public sealed class ZoneReader(byte[] buffer)
{
    private readonly ReadOnlyMemory<byte> _memory = buffer.AsMemory();
    private int _position = 0;
    private int _length = buffer.Length;
    
    private readonly IList<string> Warnings = new List<string>();

    private ReadOnlySpan<byte> Span => _memory.Span;

    public XFile ParseHeader()
    {
        var header = new XFile
        {
            Size = Span.ReadInt32(ref _position),
            ExternalSize = Span.ReadInt32(ref _position),
            BlockSize = new int[(int)XFILE_BLOCK.MAX_XFILE_COUNT]
        };
        
        for(int i = 0; i < (int)XFILE_BLOCK.MAX_XFILE_COUNT; i++)
            header.BlockSize[i] = Span.ReadInt32(ref _position);

        return header;
    }

    public XAssetList ParseXAssetList()
    {
        XAssetList assetList = ReadXAssetList();
        ResolveXAssetList(assetList);
        
        return assetList;
    }

    private XAssetList ReadXAssetList()
    {
        return new XAssetList
        {
            ScriptStringCount = Span.ReadInt32(ref _position),
            ScriptStringsPtr = ReadPointer<ZonePointer<string?>[]>(),

            AssetCount = Span.ReadInt32(ref _position),
            AssetsPtr = ReadPointer<XAsset[]>(),
        };
    }

    private void ResolveXAssetList(XAssetList assetList)
    {
        ResolveScriptStrings(assetList.ScriptStringCount, assetList.ScriptStringsPtr);

        assetList.ScriptStrings = assetList.ScriptStringsPtr.Result!
            .Select(ptr => ptr.Result)
            .ToArray();

        ResolveAssets(assetList.AssetCount, assetList.AssetsPtr);

        assetList.Assets = assetList.AssetsPtr.Result!;
    }
    
    #region Pointers
    private void ResolvePointer(Pointer ptr)
    {
        Memory.ResolvePointer(ptr, _position);
    }
    
    private ZonePointer<T> ReadPointer<T>()
    {
        return Memory.ReadPointer<T>(Span, ref _position);
    }
    #endregion
    
    #region Implementations
    private void ResolveScriptStrings(
        int count,
        ZonePointer<ZonePointer<string?>[]> scriptStringsPtr)
    {
        ResolvePointer(scriptStringsPtr);

        var stringPointers = new ZonePointer<string?>[count];

        for (int i = 0; i < count; i++)
        {
            stringPointers[i] = ReadPointer<string?>();
        }

        scriptStringsPtr.SetResult(stringPointers);

        for (int i = 0; i < count; i++)
        {
            var stringPtr = stringPointers[i];

            if (stringPtr.Kind == PointerKind.Null)
            {
                stringPtr.SetResult(null);
                continue;
            }

            ResolvePointer(stringPtr);

            _position = stringPtr.Offset;

            string value = Span.ReadCStringAt(ref _position);
            stringPtr.SetResult(value);
        }
    }
    
    private void ResolveAssets(
        int count,
        ZonePointer<XAsset[]> assetTablePtr)
    {
        ResolvePointer(assetTablePtr);

        var assets = new XAsset[count];

        for (int i = 0; i < count; i++)
        {
            assets[i] = new XAsset()
            {
                Type = (XAssetType)Span.ReadInt32(ref _position),
                XAssetPtr = ReadPointer<BaseAsset>()
            };
        }
        
        assetTablePtr.SetResult(assets);

        for (int i = 0; i < count; i++)
        {
            var asset = assets[i];

            ResolvePointer(asset.XAssetPtr);
            _position = asset.XAssetPtr.Offset;

            if (asset.XAssetPtr.Kind == PointerKind.Null)
            {
                asset.XAssetPtr.SetResult(null);
                continue;
            }

            BaseAsset value = ReadAssetHeader(asset.Type);

            if (value is UnknownAsset)
                break;
            
            asset.XAssetPtr.SetResult(value);
        }
    }

    private BaseAsset ReadAssetHeader(XAssetType type)
    {
        if (XAssetReaderRegistry.TryGetReader(type, out XAssetReader reader))
            return reader(Span, ref _position);
        
        return ReadUnknownAsset(type);
    }

    private UnknownAsset ReadUnknownAsset(XAssetType type)
    {
        Warnings.Add($"No asset reader registered for {type} at offset {_position}.");

        return new UnknownAsset(type)
        {
            Offset = _position
        };
    }
    #endregion
}
