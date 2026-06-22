using FastFile.ModelsOLD.Data;
using FastFile.ModelsOLD.Zone;

namespace FastFile.LogicOLD.Zone;

internal enum XPointerPayloadAddressMode
{
    BlockPosition,
    AllocatedBlock,
    CurrentStream
}

internal readonly record struct XPointerMaterializationPlan(
    XPointerTarget Target,
    PointerResolutionKind ResolutionKind,
    XPointerPayloadAddressMode AddressMode,
    XFILE_BLOCK PayloadBlock,
    int Alignment,
    bool ReadOffsetPayload,
    bool OffsetIsAliasCell)
{
    public static XPointerMaterializationPlan AtBlockPosition(
        XPointerTarget target,
        PointerResolutionKind resolutionKind,
        XFILE_BLOCK payloadBlock,
        bool readOffsetPayload = false,
        int alignment = 4,
        bool offsetIsAliasCell = false)
    {
        return new XPointerMaterializationPlan(
            target,
            resolutionKind,
            XPointerPayloadAddressMode.BlockPosition,
            payloadBlock,
            alignment,
            readOffsetPayload,
            offsetIsAliasCell);
    }

    public static XPointerMaterializationPlan AllocatedBlock(
        XPointerTarget target,
        PointerResolutionKind resolutionKind,
        XFILE_BLOCK payloadBlock,
        int alignment,
        bool readOffsetPayload = false,
        bool offsetIsAliasCell = false)
    {
        return new XPointerMaterializationPlan(
            target,
            resolutionKind,
            XPointerPayloadAddressMode.AllocatedBlock,
            payloadBlock,
            alignment,
            readOffsetPayload,
            offsetIsAliasCell);
    }

    public static XPointerMaterializationPlan CurrentStream(
        XPointerTarget target,
        PointerResolutionKind resolutionKind,
        int alignment = 4,
        bool readOffsetPayload = false,
        bool offsetIsAliasCell = false)
    {
        return new XPointerMaterializationPlan(
            target,
            resolutionKind,
            XPointerPayloadAddressMode.CurrentStream,
            XFILE_BLOCK.TEMP,
            alignment,
            readOffsetPayload,
            offsetIsAliasCell);
    }
}

internal readonly record struct XPointerMaterializationResult(
    PointerKind Kind,
    XBlockAddress? Address,
    bool ShouldReadPayload)
{
    public bool IsNull => Kind == PointerKind.Null;
    public bool IsOffset => Kind == PointerKind.Offset;
}
