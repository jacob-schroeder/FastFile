using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Assets.Readers;

public sealed class XSurfaceAssetReader : XAssetReadHandler
{
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

        context.ResolvePointerValue(
            surface.Verts0,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ByteArray,
                CountMember = nameof(XSurface.VertexByteCount),
                PayloadBlock = surface.Verts0InCurrentBlock ? XFILE_BLOCK.LARGE : XFILE_BLOCK.XFILE_BLOCK_VERTEX
            },
            surface);

        context.ResolvePointerValue(
            surface.Verts1,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ByteArray,
                CountMember = nameof(XSurface.VertexByteCount),
                PayloadBlock = surface.Verts1InCurrentBlock ? XFILE_BLOCK.LARGE : XFILE_BLOCK.XFILE_BLOCK_VERTEX
            },
            surface);

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

        // EBOOT 0xf8400/0xf8628/0xf8838 keep the inline payload in the
        // current stream when the matching stream flag is set. Otherwise these
        // RSX-read surface buffers live in the PS3 vertex stream block.
        context.ResolvePointerValue(
            surface.TriIndices,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ObjectArray,
                CountMember = nameof(XSurface.TriIndexCount),
                PayloadBlock = surface.TriIndicesInCurrentBlock ? XFILE_BLOCK.LARGE : XFILE_BLOCK.XFILE_BLOCK_VERTEX
            },
            surface);
    }
}
