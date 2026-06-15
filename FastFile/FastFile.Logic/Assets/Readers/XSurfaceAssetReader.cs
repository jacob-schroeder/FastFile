using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Assets.Readers;

public sealed class XSurfaceAssetReader : XAssetReadHandler
{
    private static readonly bool TraceXSurfaceEnabled = Environment.GetEnvironmentVariable("FF_TRACE_XSURFACE") == "1";

    public override bool TryResolvePointers(
        object value,
        IXAssetReaderContext context)
    {
        if (value is not XSurface surface)
            return false;

        ResolveXSurfacePointers(surface, context);
        return true;
    }

    private static void ResolveXSurfacePointers(
        XSurface surface,
        IXAssetReaderContext context)
    {
        TraceXSurface(
            context,
            surface,
            "begin");

        // PS3 Load_XSurface 0xfda50 resolves child payloads in engine order:
        // VertInfo, Verts0, Vb0 helper, Verts1, Vb1 helper, VertList,
        // TriIndices, then IndexBuffer helper. GPU buffer helpers only revisit
        // fixed root fields, so there is no extra child pointer payload here.
        context.ResolvePointerValue(
            surface.VertInfo.VertsBlend,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ObjectArray,
                CountMember = nameof(XSurfaceVertexInfo.BlendVertCount),
                PayloadBlock = XFILE_BLOCK.LARGE
            },
            surface.VertInfo);
        TraceXSurface(context, surface, "after vertsBlend");

        context.ResolvePointerValue(
            surface.Verts0,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ByteArray,
                CountMember = nameof(XSurface.VertexByteCount),
                Alignment = 16,
                PayloadBlock = surface.Verts0InCurrentBlock ? XFILE_BLOCK.LARGE : XFILE_BLOCK.PHYSICAL
            },
            surface);
        TraceXSurface(context, surface, "after verts0");

        context.ResolvePointerValue(
            surface.Verts1,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ByteArray,
                CountMember = nameof(XSurface.VertexByteCount),
                Alignment = 16,
                PayloadBlock = surface.Verts1InCurrentBlock ? XFILE_BLOCK.LARGE : XFILE_BLOCK.PHYSICAL
            },
            surface);
        TraceXSurface(context, surface, "after verts1");

        context.ResolvePointerValue(
            surface.VertList,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ObjectArray,
                CountMember = nameof(XSurface.VertListCount),
                PayloadBlock = XFILE_BLOCK.LARGE
            },
            surface);
        TraceXSurface(context, surface, "after vertList");

        // EBOOT 0xf8400/0xf8628/0xf8838 keep the inline payload in the
        // current stream when the matching stream flag is set. When the bit is
        // clear they push stream block 1 before reading, which is PHYSICAL in
        // this fastfile's block numbering, not the separate VERTEX block.
        context.ResolvePointerValue(
            surface.TriIndices,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ObjectArray,
                CountMember = nameof(XSurface.TriIndexCount),
                Alignment = 16,
                PayloadBlock = surface.TriIndicesInCurrentBlock ? XFILE_BLOCK.LARGE : XFILE_BLOCK.PHYSICAL
            },
            surface);
        TraceXSurface(context, surface, "after triIndices");
    }

    private static void TraceXSurface(
        IXAssetReaderContext context,
        XSurface surface,
        string phase)
    {
        if (!TraceXSurfaceEnabled)
            return;

        Console.Error.WriteLine(
            $"XSurfaceTrace {phase}: src=0x{context.SourcePosition:X} active={context.ActiveStreamBlock} " +
            $"temp=0x{context.GetStreamPosition(XFILE_BLOCK.TEMP):X} " +
            $"large=0x{context.GetStreamPosition(XFILE_BLOCK.LARGE):X} " +
            $"vertex=0x{context.GetStreamPosition(XFILE_BLOCK.VERTEX):X} " +
            $"off=0x{surface.Offset:X} flags=0x{surface.StreamFlags:X2} verts={surface.VertCount} tris={surface.TriCount} " +
            $"vBlendRaw=0x{surface.VertInfo.VertsBlend.Raw:X8} v0Raw=0x{surface.Verts0.Raw:X8} " +
            $"v1Raw=0x{surface.Verts1.Raw:X8} vListRaw=0x{surface.VertList.Raw:X8} triRaw=0x{surface.TriIndices.Raw:X8} " +
            $"v0Current={surface.Verts0InCurrentBlock} v1Current={surface.Verts1InCurrentBlock} triCurrent={surface.TriIndicesInCurrentBlock}");
    }
}
