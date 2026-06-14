using FastFile.Logic.Assets.Readers;
using FastFile.Logic.Extensions;
using FastFile.Models.Assets;
using FastFile.Models.Assets.AddonMapEnts;
using FastFile.Models.Assets.AiType;
using FastFile.Models.Assets.Character;
using FastFile.Models.Assets.ColMapMp;
using FastFile.Models.Assets.ColMapSp;
using FastFile.Models.Assets.ComWorld;
using FastFile.Models.Assets.Effects;
using FastFile.Models.Assets.Fonts;
using FastFile.Models.Assets.FxWorld;
using FastFile.Models.Assets.GameWorldMp;
using FastFile.Models.Assets.GameWorldSp;
using FastFile.Models.Assets.GfxLightDef;
using FastFile.Models.Assets.GfxWorld;
using FastFile.Models.Assets.ImpactFx;
using FastFile.Models.Assets.LeaderboardDef;
using FastFile.Models.Assets.Localize;
using FastFile.Models.Assets.MapEnts;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Assets.MpType;
using FastFile.Models.Assets.Physics;
using FastFile.Models.Assets.RawFiles;
using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Assets.SndDriverGlobals;
using FastFile.Models.Assets.StringTables;
using FastFile.Models.Assets.StructuredData;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Assets.Tracers;
using FastFile.Models.Assets.UiMap;
using FastFile.Models.Assets.Vehicle;
using FastFile.Models.Assets.Weapons;
using FastFile.Models.Assets.XAnim;
using FastFile.Models.Assets.XModelAlias;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Zone;

public partial class XFileReader : IXAssetReaderContext
{
    private const int XFileHeaderSize = 0x24;

    private readonly ReadOnlyMemory<byte> _memory;
    private ReadOnlySpan<byte> Span => _memory.Span;
    private int _position;

    private XFile _header = null!;
    private XBlockStream _blocks = null!;

    private XAssetList? _assetList;
    private readonly Dictionary<XBlockAddress, string?> _stringsByAddress = new();
    private readonly Dictionary<XBlockAddress, object> _objectsByAddress = new();
    private readonly Dictionary<XBlockAddress, object> _objectsByAliasCell = new();
    private readonly List<XPointer<string?>> _deferredCStringPointers = [];
    private readonly IXAssetReadHandler[] _assetReadHandlers =
    [
        new MenuAssetReader(),
        new MaterialAssetReader(),
        new XSurfaceAssetReader()
    ];
    private readonly bool _traceAssets = Environment.GetEnvironmentVariable("FF_TRACE_ASSETS") == "1";
    private readonly Action<int, int>? _assetReadProgress;
    private int _assetReadProgressTotal;
    private int _lastAssetReadProgressPercent = -1;

    public XFileReader(byte[] buffer, Action<int, int>? assetReadProgress = null)
    {
        _memory = buffer.AsMemory();
        _assetReadProgress = assetReadProgress;
    }

    XPointer<T> IXAssetReaderContext.ReadPointer<T>(PointerResolutionKind resolutionKind)
    {
        return ReadPointer<T>(resolutionKind);
    }

    XPointer<T> IXAssetReaderContext.ReinterpretPointer<T>(
        XPointer<object> pointer,
        PointerResolutionKind resolutionKind)
    {
        return ReinterpretPointer<T>(pointer, resolutionKind);
    }

    void IXAssetReaderContext.MaterializeCStringPointer(XPointer<string?> pointer)
    {
        MaterializeCStringPointer(pointer);
    }

    void IXAssetReaderContext.ResolveObjectPointers(object value)
    {
        ResolveObjectPointers(value);
    }

    void IXAssetReaderContext.ResolveChildPointers(object? value)
    {
        ResolveChildPointers(value);
    }

    void IXAssetReaderContext.ResolvePointerProperty(object owner, string propertyName)
    {
        ResolvePointerProperty(owner, propertyName);
    }

    void IXAssetReaderContext.ResolvePointerValue(
        object value,
        XPointerFieldAttribute attribute,
        object owner)
    {
        ResolvePointerValueDynamic(value, attribute, owner);
    }

    void IXAssetReaderContext.ResolveCurrentStreamObjectPointer<T>(XPointer<T> pointer)
    {
        ResolveCurrentStreamObjectPointer(pointer);
    }

    void IXAssetReaderContext.WithStreamBlock(XFILE_BLOCK block, Action action)
    {
        WithStreamBlock(block, action);
    }

    private void WithStreamBlock(XFILE_BLOCK block, Action action)
    {
        _blocks.WithBlock(block, action);
    }

    private T WithStreamBlock<T>(XFILE_BLOCK block, Func<T> func)
    {
        return _blocks.WithBlock(block, func);
    }

    private void SeekOrVerify(int expectedOffset)
    {
        _blocks.SeekOrVerify(expectedOffset);
    }

    private XPointerMaterializationResult MaterializePointer<T>(
        XPointer<T> ptr,
        XPointerMaterializationPlan plan)
    {
        return _blocks.MaterializePointer(ptr, plan);
    }

    private XPointer<T> ReadDirectPointer<T>() =>
        ReadPointer<T>(PointerResolutionKind.Direct);

    private XPointer<T> ReadAliasPointer<T>() =>
        ReadPointer<T>(PointerResolutionKind.Alias);

    private XPointer<T> ReadPointer<T>(
        PointerResolutionKind resolutionKind,
        bool patchEmittedCell = true)
    {
        int raw = Span.ReadInt32(ref _position);

        XBlockAddress? patchAddress = null;

        int patchOffset = _blocks.ActivePosition;
        _blocks.WriteInt32(raw);

        if (patchEmittedCell)
            patchAddress = new XBlockAddress(_blocks.ActiveBlockType, patchOffset);

        var pointer = XPointerCodec.CreatePointer<T>(raw, resolutionKind, patchAddress);

        ReportAssetReadProgress();
        return pointer;
    }
    
    public XFileReader DumpBlocks(string? directory = null)
    {
        const int blockCount = (int)XFILE_BLOCK.MAX_XFILE_COUNT;
        directory ??= "/Users/jacob/Downloads/streamsnew";
        Directory.CreateDirectory(directory);

        for (int i = 0; i < blockCount; i++)
        {
            string path = Path.Combine(directory, $"{(XFILE_BLOCK)i}.block");
            var block = _blocks.GetBlockSpan((XFILE_BLOCK)i).ToArray();
            
            File.WriteAllBytes(path, block);
        }

        return this;
    }

    public XFileReader Read()
    {
        Load_Header();
        ReportAssetReadProgress(force: true);
        Load_XAssetList();
        ResolveDeferredCStringPointers();
        ValidateSourcePosition();
        SealStreamBlocks();
        ReportAssetReadProgress(force: true);

        return this;
    }

    public XFileReader ReadAssetPrefix(Func<int, XAsset, bool> shouldMaterialize)
    {
        Load_Header();
        ReportAssetReadProgress(force: true);
        Load_XAssetList(shouldMaterialize);
        ResolveDeferredCStringPointers();
        ReportAssetReadProgress(force: true);

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

        for (int i = 0; i < blockCount; i++)
        {
            int blockSize = Span.ReadInt32(ref _position);

            if (blockSize < 0)
                throw new InvalidDataException($"Invalid negative block size {blockSize} for block {(XFILE_BLOCK)i}.");

            _header.BlockSize[i] = blockSize;
        }

        _assetReadProgressTotal = checked(_header.Size + XFileHeaderSize);
        _blocks = new XBlockStream(_header.BlockSize, (XFILE_BLOCK)tempBlock);
    }

    private void Load_XAssetList(Func<int, XAsset, bool>? shouldMaterialize = null)
    {
        _assetList = new XAssetList
        {
            ScriptStringCount = ReadInt32(),
            ScriptStringsPtr = ReadDirectPointer<XPointer<string?>[]>(),
            AssetCount = ReadInt32(),
            AssetsPtr = ReadDirectPointer<XAsset[]>(),
        };

        MaterializeScriptStrings(_assetList);
        MaterializeAssets(_assetList, shouldMaterialize);
    }

    private void ValidateSourcePosition()
    {
        int expectedPosition = checked(_header.Size + XFileHeaderSize);

        if (_position != expectedPosition)
        {
            throw new InvalidDataException(
                $"XFile source cursor ended at 0x{_position:X}, expected 0x{expectedPosition:X} " +
                $"from header size 0x{_header.Size:X}.");
        }
    }

    private void SealStreamBlocks()
    {
        _blocks.Seal(_header.BlockSize);
    }

    private void MaterializeScriptStrings(XAssetList list)
    {
        var ptr = list.ScriptStringsPtr;

        var materialization = MaterializePointer(
            ptr,
            XPointerMaterializationPlan.AllocatedBlock(
                XPointerTarget.PointerArray,
                ptr.ResolutionKind,
                XFILE_BLOCK.LARGE,
                alignment: 4,
                readOffsetPayload: true));

        if (materialization.IsNull)
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

    private void MaterializeAssets(
        XAssetList list,
        Func<int, XAsset, bool>? shouldMaterialize = null)
    {
        var ptr = list.AssetsPtr;
        var payloadBlock = GetAssetTablePayloadBlock(list);

        var materialization = MaterializePointer(
            ptr,
            XPointerMaterializationPlan.AllocatedBlock(
                XPointerTarget.ObjectArray,
                ptr.ResolutionKind,
                payloadBlock,
                alignment: 4,
                readOffsetPayload: true));

        if (materialization.IsNull)
        {
            ptr.Value = [];
            return;
        }

        void ReadAssets()
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

                    XAssetPtr = ReadAliasPointer<BaseAsset>()
                };
            }

            // Phase 2: now _position points at the first asset payload.
            for (int i = 0; i < assets.Length; i++)
            {
                if (shouldMaterialize is not null &&
                    !shouldMaterialize(i, assets[i]))
                {
                    break;
                }

                MaterializeAsset(assets[i], i);
                ReportAssetReadProgress();
            }

            ptr.Value = assets;
        }

        if (ptr.Address!.Value.Block == _blocks.ActiveBlockType)
            ReadAssets();
        else
            WithStreamBlock(ptr.Address.Value.Block, ReadAssets);
    }

    private XFILE_BLOCK GetAssetTablePayloadBlock(XAssetList list)
    {
        // PS3 EBOOT 0x1167c0 keeps the XAsset array under the surrounding LARGE
        // push after the script-string TEMP subpath returns; there is no TEMP/LARGE
        // heuristic here.
        return XFILE_BLOCK.LARGE;
    }

    private void MaterializeAsset(XAsset asset, int index)
    {
        var ptr = asset.XAssetPtr;
        int startPosition = _position;

        if (ptr.Kind == PointerKind.Null)
        {
            ptr.Value = null;
            return;
        }

        try
        {
            ptr.Value = LoadAssetByType(asset.Type, ptr);
            if (_traceAssets)
            {
                Console.Error.WriteLine(
                    $"asset[{index}] {asset.Type} src=0x{startPosition:X}-0x{_position:X} " +
                    $"root={ptr.Kind}/{ptr.Address} temp=0x{_blocks.GetPosition(XFILE_BLOCK.TEMP):X} " +
                    $"large=0x{_blocks.GetPosition(XFILE_BLOCK.LARGE):X}");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidDataException(
                $"Failed to load asset[{index}] {asset.Type} from pointer 0x{ptr.Raw:X8}.",
                ex);
        }
        finally
        {
            ReportAssetReadProgress();
        }
    }
    
    private BaseAsset LoadAssetByType(XAssetType type, XPointer<BaseAsset> ptr)
    {
        return type switch
        {
            XAssetType.PhysPreset => LoadAssetRoot<PhysPreset>(ptr),
            XAssetType.PhysCollmap => LoadAssetRoot<PhysCollmap>(ptr),
            XAssetType.XAnim => LoadAssetRoot<XAnimParts>(ptr),
            XAssetType.XModelSurfs => LoadAssetRoot<XModelSurfs>(ptr),
            XAssetType.XModel => LoadAssetRoot<XModel>(ptr),
            XAssetType.Material => LoadAssetRoot<Material>(ptr),
            XAssetType.PixelShader => LoadAssetRoot<MaterialPixelShader>(ptr),
            XAssetType.VertexShader => LoadAssetRoot<MaterialVertexShader>(ptr),
            XAssetType.Techset => Load_MaterialTechniqueSetAsset(ptr),
            XAssetType.Image => LoadAssetRoot<GfxImage>(ptr),
            XAssetType.Sound => LoadAssetRoot<SndAliasList>(ptr),
            XAssetType.SndCurve => LoadAssetRoot<SndCurve>(ptr),
            XAssetType.LoadedSound => LoadAssetRoot<LoadedSound>(ptr),
            XAssetType.ColMapSp => LoadAssetRoot<ColMapSp>(ptr),
            XAssetType.ColMapMp => LoadAssetRoot<ColMapMp>(ptr),
            XAssetType.ComMap => LoadAssetRoot<ComWorld>(ptr),
            XAssetType.GameMapSp => LoadAssetRoot<GameWorldSp>(ptr),
            XAssetType.GameMapMp => LoadAssetRoot<GameWorldMp>(ptr),
            XAssetType.MapEnts => LoadAssetRoot<MapEnts>(ptr),
            XAssetType.FxMap => LoadAssetRoot<FxWorld>(ptr),
            XAssetType.GfxMap => LoadAssetRoot<GfxWorld>(ptr),
            XAssetType.LightDef => LoadAssetRoot<GfxLightDef>(ptr),
            XAssetType.UiMap => LoadAssetRoot<UiMap>(ptr),
            XAssetType.Font => LoadAssetRoot<FontAsset>(ptr),
            XAssetType.Localize => LoadAssetRoot<LocalizeEntry>(ptr),
            XAssetType.Weapon => LoadAssetRoot<WeaponVariantDef>(ptr),
            XAssetType.SndDriverGlobals => LoadAssetRoot<SndDriverGlobals>(ptr),
            XAssetType.Fx => LoadAssetRoot<FxEffectDef>(ptr),
            XAssetType.ImpactFx => LoadAssetRoot<FxImpactTable>(ptr),
            XAssetType.AiType => LoadAssetRoot<AiType>(ptr),
            XAssetType.MpType => LoadAssetRoot<MpType>(ptr),
            XAssetType.Character => LoadAssetRoot<Character>(ptr),
            XAssetType.XModelAlias => LoadAssetRoot<XModelAlias>(ptr),
            XAssetType.RawFile => LoadAssetRoot<RawFile>(ptr),
            XAssetType.StringTable => LoadAssetRoot<StringTable>(ptr),
            XAssetType.LeaderboardDef => LoadAssetRoot<LeaderboardDef>(ptr),
            XAssetType.StructuredDataDef => LoadAssetRoot<StructuredDataDefSet>(ptr),
            XAssetType.Tracer => LoadAssetRoot<TracerDef>(ptr),
            XAssetType.Vehicle => LoadAssetRoot<VehicleDef>(ptr),
            XAssetType.AddonMapEnts => LoadAssetRoot<AddonMapEnts>(ptr),
            XAssetType.MenuFile => LoadAssetRoot<MenuList>(ptr),
            XAssetType.Menu => LoadAssetRoot<MenuDef>(ptr),
            _ => throw new InvalidDataException($"Unknown XAssetType value {(int)type}.")
        };
    }
    
    private T LoadAssetRoot<T>(XPointer<BaseAsset> assetPtr)
        where T : BaseAsset, new()
    {
        MaterializePointer(
            assetPtr,
            XPointerMaterializationPlan.AtBlockPosition(
                XPointerTarget.Object,
                assetPtr.ResolutionKind,
                XFILE_BLOCK.TEMP,
                readOffsetPayload: true));

        return WithStreamBlock(assetPtr.Address!.Value.Block, () =>
        {
            var address = assetPtr.Address.Value;
            if (TryGetCachedObject<T>(address, out var cached))
            {
                CacheAliasCellObject(assetPtr.PatchAddress, cached);
                return cached;
            }

            SeekOrVerify(address.Offset);

            var obj = new T
            {
                Offset = address.Offset
            };

            ReadObjectFields(obj);
            CacheObject(address, obj);
            CacheAliasCellObject(assetPtr.PatchAddress, obj);
            ResolveLoadedObjectPointers(obj);

            return obj;
        });
    }

    // PS3 top-level Techset asset family entrypoint.
    private MaterialTechniqueSet Load_MaterialTechniqueSetAsset(
        XPointer<BaseAsset> assetPtr)
    {
        return LoadAssetRoot<MaterialTechniqueSet>(assetPtr);
    }

    private void MaterializeCStringPointer(XPointer<string?> ptr)
    {
        MaterializeCStringPointer(
            ptr,
            XPointerMaterializationPlan.AtBlockPosition(
                XPointerTarget.CString,
                ptr.ResolutionKind,
                _blocks.ActiveBlockType));
    }

    private void MaterializeCStringPointer(
        XPointer<string?> ptr,
        XPointerMaterializationPlan plan)
    {
        var materialization = MaterializePointer(ptr, plan);

        if (materialization.IsNull)
        {
            ptr.Value = null;
            return;
        }

        if (!materialization.ShouldReadPayload)
        {
            if (materialization.Address is { } address && TryGetEmittedString(address, out var value))
            {
                ptr.Value = value;
            }
            else
            {
                ptr.Value = null;
                _deferredCStringPointers.Add(ptr);
            }

            return;
        }

        XBlockAddress payloadAddress = materialization.Address
                                       ?? throw new InvalidDataException("CString pointer materialized without an address.");

        WithStreamBlock(payloadAddress.Block, () =>
        {
            SeekOrVerify(payloadAddress.Offset);
            ptr.Value = ReadCString();
            _stringsByAddress[payloadAddress] = ptr.Value;
        });
    }

    private void ResolveDeferredCStringPointers()
    {
        foreach (var ptr in _deferredCStringPointers)
        {
            if (ptr.Address is { } address && TryGetEmittedString(address, out var value))
                ptr.Value = value;
        }

        _deferredCStringPointers.Clear();
    }

    private int ReadInt32()
    {
        int value = Span.ReadInt32(ref _position);
        _blocks.WriteInt32(value);
        ReportAssetReadProgress();
        return value;
    }

    private byte[] ReadBytes(int count)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative read length {count}.");

        byte[] value = Span.Slice(_position, count).ToArray();
        _position += count;
        _blocks.Write(value);
        ReportAssetReadProgress();
        return value;
    }

    private string ReadCString()
    {
        string value = Span.ReadCStringAt(ref _position);
        _blocks.WriteCString(value);
        ReportAssetReadProgress();
        return value;
    }

    private void ReportAssetReadProgress(bool force = false)
    {
        if (_assetReadProgress is null || _assetReadProgressTotal <= 0)
            return;

        int unitsRead = Math.Clamp(_position, 0, _assetReadProgressTotal);
        int percent = Math.Clamp((int)Math.Round(unitsRead * 100d / _assetReadProgressTotal), 0, 100);

        if (!force && percent <= _lastAssetReadProgressPercent)
            return;

        _lastAssetReadProgressPercent = percent;
        _assetReadProgress(unitsRead, _assetReadProgressTotal);
    }

    private bool TryGetEmittedString(XBlockAddress address, out string? value)
    {
        if (_stringsByAddress.TryGetValue(address, out value))
            return true;

        if (!_blocks.ContainsBlock(address.Block))
        {
            value = null;
            return false;
        }

        var span = _blocks.GetWrittenSpan(address.Block);
        if (address.Offset < 0 || address.Offset >= span.Length)
        {
            value = null;
            return false;
        }

        try
        {
            int offset = address.Offset;
            value = span.ReadCStringAt(ref offset);
            _stringsByAddress[address] = value;
            return true;
        }
        catch (InvalidDataException)
        {
            value = null;
            return false;
        }
    }

    private void CacheObject(XBlockAddress address, object value)
    {
        if (address.Block == XFILE_BLOCK.TEMP)
            return;

        _objectsByAddress[address] = value;
    }

    private void CacheAliasCellObject(XBlockAddress? aliasCellAddress, object value)
    {
        if (aliasCellAddress is not { } address)
            return;

        _objectsByAliasCell[address] = value;
    }

    private bool TryGetCachedObject<T>(XBlockAddress address, out T value)
        where T : class
    {
        if (address.Block == XFILE_BLOCK.TEMP)
        {
            value = null!;
            return false;
        }

        if (_objectsByAddress.TryGetValue(address, out var cached) && cached is T typed)
        {
            value = typed;
            return true;
        }

        value = null!;
        return false;
    }

    private bool TryGetCachedObject(Type targetType, XBlockAddress address, out object value)
    {
        if (address.Block == XFILE_BLOCK.TEMP)
        {
            value = null!;
            return false;
        }

        if (_objectsByAddress.TryGetValue(address, out var cached) &&
            targetType.IsInstanceOfType(cached))
        {
            value = cached;
            return true;
        }

        value = null!;
        return false;
    }

    private bool TryGetCachedAliasedObject(Type targetType, Pointer pointer, out object value)
    {
        if (pointer.Kind != PointerKind.Offset)
        {
            value = null!;
            return false;
        }

        var aliasCellAddress = XPointerCodec.DecodeOffset(pointer.Raw);
        if (_objectsByAliasCell.TryGetValue(aliasCellAddress, out var cached) &&
            targetType.IsInstanceOfType(cached))
        {
            value = cached;
            return true;
        }

        value = null!;
        return false;
    }

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

    public int GetSourcePosition()
    {
        return _position;
    }

    public int[] GetWrittenBlockSizes()
    {
        return _blocks.GetWrittenBlockSizes();
    }

    #endregion
}
