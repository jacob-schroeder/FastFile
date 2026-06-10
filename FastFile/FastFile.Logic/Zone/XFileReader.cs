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

namespace FastFile.Logic.Zone;

public partial class XFileReader
{
    private const int XFileHeaderSize = 0x24;

    private readonly ReadOnlyMemory<byte> _memory;
    private ReadOnlySpan<byte> Span => _memory.Span;
    private int _position;

    private XFile _header = null!;
    private XBlock[] _streamBlocks = [];
    private readonly Stack<StreamBlockFrame> _blockStack = new();
    private XBlock _activeBlock = null!;

    private XAssetList? _assetList;
    private readonly Dictionary<XBlockAddress, string?> _stringsByAddress = new();
    private readonly Dictionary<XBlockAddress, object> _objectsByAddress = new();
    private readonly List<XPointer<string?>> _deferredCStringPointers = [];
    private readonly bool _traceAssets = Environment.GetEnvironmentVariable("FF_TRACE_ASSETS") == "1";
    private readonly Action<int, int>? _assetReadProgress;
    private int _assetReadProgressTotal;
    private int _lastAssetReadProgressPercent = -1;

    private readonly record struct StreamBlockFrame(
        XFILE_BLOCK PreviousBlock,
        XFILE_BLOCK PushedBlock,
        int PushedPosition);

    public XFileReader(byte[] buffer, Action<int, int>? assetReadProgress = null)
    {
        _memory = buffer.AsMemory();
        _assetReadProgress = assetReadProgress;
    }
    
    public XFileReader DumpBlocks(string? directory = null)
    {
        const int blockCount = (int)XFILE_BLOCK.MAX_XFILE_COUNT;
        directory ??= "/Users/jacob/Downloads/streamsnew";
        Directory.CreateDirectory(directory);

        for (int i = 0; i < blockCount; i++)
        {
            string path = Path.Combine(directory, $"{(XFILE_BLOCK)i}.block");
            var block = _streamBlocks[i].BlockSpan.ToArray();
            
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

        _assetReadProgressTotal = checked(_header.Size + XFileHeaderSize);

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
        for (int i = 0; i < _streamBlocks.Length; i++)
        {
            var block = _streamBlocks[i];
            var expectedSize = _header.BlockSize[i];

            if (block.WrittenSpan.Length > expectedSize)
            {
                throw new InvalidDataException(
                    $"{block.BlockType} stream length 0x{block.WrittenSpan.Length:X} exceeds header block size 0x{expectedSize:X}.");
            }

            block.PadToCapacity();
        }
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

                    XAssetPtr = ReadAliasPointer<BaseAsset>()
                };
            }

            // Phase 2: now _position points at the first asset payload.
            for (int i = 0; i < assets.Length; i++)
            {
                MaterializeAsset(assets[i], i);
                ReportAssetReadProgress();
            }

            ptr.Value = assets;
        });
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
                    $"root={ptr.Kind}/{ptr.Address} temp=0x{_streamBlocks[(int)XFILE_BLOCK.TEMP].Position:X} " +
                    $"large=0x{_streamBlocks[(int)XFILE_BLOCK.LARGE].Position:X}");
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
            XAssetType.Techset => LoadAssetRoot<MaterialTechniqueSet>(ptr),
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
        TryMaterializePointer(
            assetPtr,
            () => new XBlockAddress(
                XFILE_BLOCK.TEMP,
                _streamBlocks[(int)XFILE_BLOCK.TEMP].Position));

        return WithStreamBlock(assetPtr.Address!.Value.Block, () =>
        {
            var address = assetPtr.Address.Value;
            if (TryGetCachedObject<T>(address, out var cached))
                return cached;

            SeekOrVerify(address.Offset);

            var obj = new T
            {
                Offset = address.Offset
            };

            ReadObjectFields(obj);
            CacheObject(address, obj);
            ResolveLoadedObjectPointers(obj);

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

        if (ptr.Kind == PointerKind.Offset)
        {
            if (ptr.Address is { } address && TryGetEmittedString(address, out var value))
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

        WithStreamBlock(ptr.Address!.Value.Block, () =>
        {
            SeekOrVerify(ptr.Address.Value.Offset);
            ptr.Value = ReadCString();
            _stringsByAddress[ptr.Address.Value] = ptr.Value;
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
        _activeBlock.WriteInt32(value);
        ReportAssetReadProgress();
        return value;
    }

    private byte[] ReadBytes(int count)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative read length {count}.");

        byte[] value = Span.Slice(_position, count).ToArray();
        _position += count;
        _activeBlock.Write(value);
        ReportAssetReadProgress();
        return value;
    }

    private string ReadCString()
    {
        string value = Span.ReadCStringAt(ref _position);
        _activeBlock.WriteCString(value);
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

        int blockIndex = (int)address.Block;
        if (blockIndex < 0 || blockIndex >= _streamBlocks.Length)
        {
            value = null;
            return false;
        }

        var span = _streamBlocks[blockIndex].WrittenSpan;
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
