using FastFile.Models.Assets.TechniqueSet;
using FastFile.Logic.Streams;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Logic;

public partial class XFileReader
{
    private readonly SourceReader _source;
    private readonly Dictionary<XFileBlockType, IBlockCursor> _blockCursors = new();
    private XFileBlockStack _blocks = null!;
    private MirroredReadCursor _reader = null!;
    private EngineLoadContext _context = null!;

    public XFile Header { get; private set; }
    public XFileBlockStack Blocks => _blocks;
    public XAssetList XAssetList { get; private set; }

    public XFileReader(byte[] zone)
    {
        _source = new SourceReader(zone);
        
        ReadHeader();
        SetupBlockStreams();
        Load_XAssetList();
        DumpBlocks("/Users/jacob/Repositories/FastFile/Data/dump");
    }

    private void ReadHeader()
    {
        Header = new XFile
        {
            Size = _source.ReadInt32(),
            ExternalSize = _source.ReadInt32(),
            BlockSize = new int[(int)XFileBlockType.COUNT]
        };

        for (int i = 0; i < (int)XFileBlockType.COUNT; i++)
            Header.BlockSize[i] = _source.ReadInt32();
    }

    private void Load_XAssetList()
    {
        XAssetList list;

        using (Blocks.Push(XFileBlockType.TEMP))
        {
            list = _context.ReadStruct<XAssetList>();
        }

        using (Blocks.Push(XFileBlockType.LARGE))
        {
            Load_ScriptStringList(ref list);
            Load_XAssetArray(ref list);
        }

        XAssetList = list;
    }

    private void Load_ScriptStringList(ref XAssetList list)
    {
        PointerCell strings = _context.TakePointerCell();

        if (!_context.TryPatchPointerToCurrentBlock(strings, out int patchedPointer))
            return;

        list.ScriptStrings.Value = patchedPointer;
        Load_ScriptStringArray(list.ScriptStringCount);
    }

    private void Load_ScriptStringArray(int count)
    {
        PointerCell[] stringPointers = _context.Load_PointerArray<XPointer<string>>(count);

        foreach (PointerCell stringPointer in stringPointers)
        {
            if (!_context.TryPatchPointerToCurrentBlock(stringPointer, out _))
                continue;

            _context.Load_CString();
        }
    }

    private void Load_XAssetArray(ref XAssetList list)
    {
        PointerCell assets = _context.TakePointerCell();

        if (!_context.TryPatchPointerToCurrentBlock(assets, out int patchedPointer))
        {
            return;
        }

        list.Assets.Value = patchedPointer;

        var assetArray = new XAsset[list.AssetCount];
        var assetHeaders = new PointerCell[list.AssetCount];

        for (int i = 0; i < list.AssetCount; i++)
        {
            assetArray[i] = _context.ReadStruct<XAsset>();
            assetHeaders[i] = _context.TakePointerCell();
        }

        using (Blocks.Push(XFileBlockType.TEMP))
        {
            for (int i = 0; i < assetArray.Length; i++)
                Load_XAssetHeader(assetHeaders[i]);
        }

        if (assetArray.Length > 0)
            Load_XAsset(assetArray[0]);
    }

    private void Load_XAsset(XAsset asset)
    {
        switch (asset.Type)
        {
            case XAssetType.Techset:
                Load_MaterialTechniqueSet();
                break;

            case XAssetType.PhysPreset:
            case XAssetType.PhysCollmap:
            case XAssetType.XAnim:
            case XAssetType.XModelSurfs:
            case XAssetType.XModel:
            case XAssetType.Material:
            case XAssetType.PixelShader:
            case XAssetType.VertexShader:
            case XAssetType.Image:
            case XAssetType.Sound:
            case XAssetType.SndCurve:
            case XAssetType.LoadedSound:
            case XAssetType.ColMapSp:
            case XAssetType.ColMapMp:
            case XAssetType.ComMap:
            case XAssetType.GameMapSp:
            case XAssetType.GameMapMp:
            case XAssetType.MapEnts:
            case XAssetType.FxMap:
            case XAssetType.GfxMap:
            case XAssetType.LightDef:
            case XAssetType.UiMap:
            case XAssetType.Font:
            case XAssetType.MenuFile:
            case XAssetType.Menu:
            case XAssetType.Localize:
            case XAssetType.Weapon:
            case XAssetType.SndDriverGlobals:
            case XAssetType.Fx:
            case XAssetType.ImpactFx:
            case XAssetType.AiType:
            case XAssetType.MpType:
            case XAssetType.Character:
            case XAssetType.XModelAlias:
            case XAssetType.RawFile:
            case XAssetType.StringTable:
            case XAssetType.LeaderboardDef:
            case XAssetType.StructuredDataDef:
            case XAssetType.Tracer:
            case XAssetType.Vehicle:
            case XAssetType.AddonMapEnts:
            default:
                throw new NotImplementedException();
        }
    }

    private void Load_MaterialTechniqueSet()
    {
        using (Blocks.Push(XFileBlockType.TEMP))
        {
            _context.ReadStruct<MaterialTechniqueSet>();
        }

        using (Blocks.Push(XFileBlockType.LARGE))
        {
            _context.Load_XString(_context.TakePointerCell());

            for (int i = 0; i < MaterialTechniqueSet.TechniqueCount; i++)
                _context.TakePointerCell();
        }
    }

    private void Load_XAssetHeader(PointerCell assetHeader)
    {
        _context.TryPatchPointerToCurrentBlock(assetHeader, out _);
    }
}
