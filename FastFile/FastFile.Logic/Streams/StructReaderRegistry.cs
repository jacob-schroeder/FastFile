using FastFile.Models.Assets;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;

namespace FastFile.Logic.Streams;

internal static class StructReaderRegistry
{
    private static readonly IReadOnlyDictionary<Type, Func<IReadCursor, object>> Readers =
        new Dictionary<Type, Func<IReadCursor, object>>
        {
            [typeof(XAssetList)] = cursor => ReadXAssetList(cursor),
            [typeof(XAsset)] = cursor => ReadXAsset(cursor),
            [typeof(MaterialTechniqueSet)] = cursor => ReadMaterialTechniqueSet(cursor)
        };

    public static T Read<T>(IReadCursor cursor) where T : struct
    {
        if (Readers.TryGetValue(typeof(T), out Func<IReadCursor, object>? reader))
            return (T)reader(cursor);

        throw new NotImplementedException(
            $"No {nameof(IReadCursor.ReadStruct)} reader is registered for {typeof(T).FullName}.");
    }

    private static XAssetList ReadXAssetList(IReadCursor cursor)
    {
        return new XAssetList
        {
            ScriptStringCount = cursor.ReadInt32(),
            ScriptStrings = ReadArray<XPointer<string>>(cursor),
            AssetCount = cursor.ReadInt32(),
            Assets = ReadArray<XAsset>(cursor)
        };
    }

    private static XAsset ReadXAsset(IReadCursor cursor)
    {
        return new XAsset
        {
            Type = (XAssetType)cursor.ReadInt32(),
            Asset = ReadPointer<BaseAsset>(cursor)
        };
    }

    private static MaterialTechniqueSet ReadMaterialTechniqueSet(IReadCursor cursor)
    {
        var techniques = new XPointer<MaterialTechnique>[MaterialTechniqueSet.TechniqueCount];

        var value = new MaterialTechniqueSet
        {
            Name = ReadPointer<string>(cursor),
            WorldVertexFormat = (MaterialWorldVertexFormat)cursor.ReadByte(),
            Techniques = techniques
        };

        cursor.ReadByte();
        cursor.ReadByte();
        cursor.ReadByte();

        for (int i = 0; i < techniques.Length; i++)
            techniques[i] = ReadPointer<MaterialTechnique>(cursor);

        return value;
    }

    private static XArray<T> ReadArray<T>(IReadCursor cursor)
    {
        XBlockAddress? address = TryGetPointerCellAddress(cursor);
        int raw = cursor.ReadInt32();
        TryRecordPointerCell(cursor, address, raw);

        return new XArray<T>
        {
            Value = raw
        };
    }

    private static XPointer<T> ReadPointer<T>(IReadCursor cursor)
    {
        XBlockAddress? address = TryGetPointerCellAddress(cursor);
        int raw = cursor.ReadInt32();
        TryRecordPointerCell(cursor, address, raw);

        return new XPointer<T>(raw);
    }

    private static XBlockAddress? TryGetPointerCellAddress(IReadCursor cursor)
    {
        return cursor is IPointerCellRecorder recorder
            ? recorder.CurrentWriteAddress
            : null;
    }

    private static void TryRecordPointerCell(
        IReadCursor cursor,
        XBlockAddress? address,
        int raw)
    {
        if (cursor is IPointerCellRecorder recorder && address is { } cellAddress)
            recorder.RecordPointerCell(cellAddress, raw);
    }
}
