using FastFile.Logic.Assets.Readers.Generic;
using FastFile.Logic.Zone;
using FastFile.Models.Assets.XModels;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;

namespace FastFile.Logic.Assets.Readers;

internal static class XModelSurfsReader
{
    private const int XSurfaceSize = 0x54;
    private const int XSurfaceVertexInfoSize = 0x0c;
    private const int XSurfaceVertexPayloadStride = 0x10;
    private const int XRigidVertListSize = 0x0c;
    private const int XSurfaceCollisionTreeSize = 0x28;
    private const int XSurfaceCollisionNodeSize = 0x10;
    private const int XSurfaceCollisionLeafSize = 0x02;

    private const byte XSurfaceStreamFlagVerts0 = 0x01;
    private const byte XSurfaceStreamFlagVerts1 = 0x02;
    private const byte XSurfaceStreamFlagTriIndices = 0x04;
    private static readonly bool TraceXSurfs = IsTraceEnabled("FASTFILE_TRACE_XSURFS");
    private static readonly int TraceXSurfsStart = GetTraceInt("FASTFILE_TRACE_XSURFS_START", 0);
    private static readonly int TraceXSurfsEnd = GetTraceInt("FASTFILE_TRACE_XSURFS_END", int.MaxValue);
    private static readonly int TraceXSurfsLimit = GetTraceInt("FASTFILE_TRACE_XSURFS_LIMIT", 4096);
    private static int _traceXSurfsCount;

    public static XModelSurfs Read(ref XFileReadContext context)
    {
        var asset = new XModelSurfs
        {
            Offset = context.Position,
            NamePtr = GenericReader.ReadStringPointer(ref context, resolve: false),
            Surfs = context.ReadDirectPointer<XSurface[]>("XModelSurfs.Surfs"),
        };

        asset.NumSurfs = context.ReadUInt16();
        asset.PartBitsAlignment = context.ReadUInt16();
        for (var i = 0; i < asset.PartBits.Length; i++)
            asset.PartBits[i] = context.ReadInt32();

        Trace(
            ref context,
            $"xmodelsurfs root src=0x{asset.Offset:X8} end=0x{context.Position:X8} "
            + $"nameRaw=0x{asset.NamePtr.Raw:X8} surfsRaw=0x{asset.Surfs.Raw:X8} numSurfs={asset.NumSurfs} partBitsAlign=0x{asset.PartBitsAlignment:X4}");

        context.PushStreamBlock(XFILE_BLOCK.LARGE);
        try
        {
            GenericReader.ResolveStringPointerNow(ref context, asset.NamePtr);
            ResolveXSurfaceArray(ref context, asset.Surfs, asset.NumSurfs);
        }
        finally
        {
            context.PopStreamBlock();
        }

        return asset;
    }

    public static ZonePointer<XModelSurfs> ReadXModelSurfsPointer(ref XFileReadContext context)
    {
        var pointer = context.ReadAliasPointer<XModelSurfs>("XModelSurfsAssetRef");
        context.ResolvePointerInBlock(
            pointer,
            XFILE_BLOCK.TEMP,
            (ref XFileReadContext pointerContext, ZonePointer<XModelSurfs> pointer) =>
            {
                pointer.SetResult(pointerContext.ReadPointerValue(pointer, Read));
            });
        return pointer;
    }

    public static void ResolveXModelSurfsPointerNow(
        ref XFileReadContext context,
        ZonePointer<XModelSurfs> pointer)
    {
        context.ResolvePointerNowInBlock(pointer, XFILE_BLOCK.TEMP, ReadXModelSurfsPointerValue);
    }

    public static void ReadXModelSurfsPointerValue(
        ref XFileReadContext context,
        ZonePointer<XModelSurfs> pointer)
    {
        pointer.SetResult(context.ReadPointerValue(pointer, Read));
    }

    private static void ResolveXSurfaceArray(
        ref XFileReadContext context,
        ZonePointer<XSurface[]> pointer,
        int count)
    {
        if (count <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(
            pointer,
            (ref XFileReadContext pointerContext, ZonePointer<XSurface[]> p) =>
            {
                var start = pointerContext.Position;
                Trace(ref pointerContext, $"xsurface[] begin src=0x{start:X8} count={count} raw=0x{p.Raw:X8}");
                p.SetResult(pointerContext.ReadPointerValue(
                    p,
                    (ref XFileReadContext valueContext) =>
                    {
                        var values = new XSurface[count];
                        for (var i = 0; i < values.Length; i++)
                            values[i] = ReadXSurface(ref valueContext);

                        return values;
                    }));

                if (p.Result is null)
                    return;

                foreach (var surface in p.Result)
                    ResolveXSurfaceChildren(ref pointerContext, surface);

                Trace(
                    ref pointerContext,
                    $"xsurface[] end src=0x{start:X8} end=0x{pointerContext.Position:X8} len=0x{pointerContext.Position - start:X}");
            });
    }

    private static XSurface ReadXSurface(ref XFileReadContext context)
    {
        var start = context.Position;
        var surface = new XSurface
        {
            Offset = start,
            TileMode = context.ReadByte(),
            Deformed = context.ReadByte(),
            StreamFlags = context.ReadByte(),
            Unknown03 = context.ReadByte(),
            VertCount = context.ReadUInt16(),
            TriCount = context.ReadUInt16(),
            TriIndices = context.ReadDirectPointer<ushort[]>("XSurface.TriIndices"),
            VertInfo = ReadXSurfaceVertexInfo(ref context),
            Verts0 = context.ReadDirectPointer<byte[]>("XSurface.Verts0"),
            Vb0 = ReadGpuBuffer(ref context),
            Verts1 = context.ReadDirectPointer<byte[]>("XSurface.Verts1"),
            Vb1 = ReadGpuBuffer(ref context),
            VertListCount = context.ReadInt32(),
            VertList = context.ReadDirectPointer<XRigidVertList[]>("XSurface.VertList"),
            IndexBuffer = ReadGpuBuffer(ref context),
        };

        for (var i = 0; i < surface.PartBits.Length; i++)
            surface.PartBits[i] = context.ReadInt32();

        var bytesRead = context.Position - start;
        if (bytesRead != XSurfaceSize)
            throw new InvalidDataException($"XSurface read {bytesRead:N0} bytes; expected {XSurfaceSize:N0} bytes.");

        Trace(
            ref context,
            $"surface root src=0x{start:X8} end=0x{context.Position:X8} "
            + $"flags=0x{surface.StreamFlags:X2} verts={surface.VertCount} tris={surface.TriCount} "
            + $"triRaw=0x{surface.TriIndices.Raw:X8} blendRaw=0x{surface.VertInfo.VertsBlend.Raw:X8} "
            + $"blendCounts={surface.VertInfo.VertCount[0]},{surface.VertInfo.VertCount[1]},{surface.VertInfo.VertCount[2]},{surface.VertInfo.VertCount[3]} "
            + $"verts0Raw=0x{surface.Verts0.Raw:X8} verts1Raw=0x{surface.Verts1.Raw:X8} "
            + $"vertListCount={surface.VertListCount} vertListRaw=0x{surface.VertList.Raw:X8} "
            + $"idxBuffer=0x{surface.IndexBuffer.Word0:X8}/0x{surface.IndexBuffer.Word1:X8}");
        return surface;
    }

    private static XSurfaceVertexInfo ReadXSurfaceVertexInfo(ref XFileReadContext context)
    {
        var start = context.Position;
        var value = new XSurfaceVertexInfo();
        for (var i = 0; i < value.VertCount.Length; i++)
            value.VertCount[i] = unchecked((short)context.ReadUInt16());

        value.VertsBlend = context.ReadDirectPointer<ushort[]>("XSurface.VertexInfo.VertsBlend");

        var bytesRead = context.Position - start;
        if (bytesRead != XSurfaceVertexInfoSize)
            throw new InvalidDataException($"XSurfaceVertexInfo read {bytesRead:N0} bytes; expected {XSurfaceVertexInfoSize:N0} bytes.");

        return value;
    }

    private static XSurfaceGpuBuffer ReadGpuBuffer(ref XFileReadContext context)
    {
        return new XSurfaceGpuBuffer
        {
            Word0 = context.ReadInt32(),
            Word1 = context.ReadInt32(),
        };
    }

    private static void ResolveXSurfaceChildren(ref XFileReadContext context, XSurface surface)
    {
        var start = context.Position;
        Trace(ref context, $"surface children begin root=0x{surface.Offset:X8} src=0x{start:X8}");
        var childStart = context.Position;
        ResolveUShortArray(ref context, surface.VertInfo.VertsBlend, GetVertsBlendCount(surface.VertInfo));
        TraceSpan(ref context, "  vertsBlend", childStart);
        childStart = context.Position;
        ResolveBytePayload(
            ref context,
            surface.Verts0,
            checked(surface.VertCount * XSurfaceVertexPayloadStride),
            GetSurfacePayloadBlock(surface, XSurfaceStreamFlagVerts0));
        TraceSpan(ref context, "  verts0", childStart);
        childStart = context.Position;
        ResolveBytePayload(
            ref context,
            surface.Verts1,
            checked(surface.VertCount * XSurfaceVertexPayloadStride),
            GetSurfacePayloadBlock(surface, XSurfaceStreamFlagVerts1));
        TraceSpan(ref context, "  verts1", childStart);
        childStart = context.Position;
        ResolveRigidVertListArray(ref context, surface.VertList, surface.VertListCount);
        TraceSpan(ref context, "  vertList", childStart);
        childStart = context.Position;
        ResolveUShortArray(
            ref context,
            surface.TriIndices,
            checked(surface.TriCount * 3),
            GetSurfacePayloadBlock(surface, XSurfaceStreamFlagTriIndices));
        TraceSpan(ref context, "  triIndices", childStart);
        Trace(ref context, $"surface children end root=0x{surface.Offset:X8} src=0x{start:X8} end=0x{context.Position:X8} len=0x{context.Position - start:X}");
    }

    private static XFILE_BLOCK? GetSurfacePayloadBlock(XSurface surface, byte streamFlag)
    {
        return (surface.StreamFlags & streamFlag) == 0
            ? XFILE_BLOCK.PHYSICAL
            : null;
    }

    private static int GetVertsBlendCount(XSurfaceVertexInfo info)
    {
        return checked(
            Math.Max(0, (int)info.VertCount[0])
            + (Math.Max(0, (int)info.VertCount[1]) * 3)
            + (Math.Max(0, (int)info.VertCount[2]) * 5)
            + (Math.Max(0, (int)info.VertCount[3]) * 7));
    }

    private static void ResolveUShortArray(
        ref XFileReadContext context,
        ZonePointer<ushort[]> pointer,
        int count,
        XFILE_BLOCK? block = null)
    {
        if (count <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        ResolveInlinePointerNow(
            ref context,
            pointer,
            block,
            (ref XFileReadContext pointerContext, ZonePointer<ushort[]> p) =>
            {
                p.SetResult(pointerContext.ReadPointerValue(
                    p,
                    (ref XFileReadContext valueContext) =>
                    {
                        var values = new ushort[count];
                        for (var i = 0; i < values.Length; i++)
                            values[i] = valueContext.ReadUInt16();

                        return values;
                    }));
            });
    }

    private static void ResolveBytePayload(
        ref XFileReadContext context,
        ZonePointer<byte[]> pointer,
        int byteCount,
        XFILE_BLOCK? block = null)
    {
        if (byteCount <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        ResolveInlinePointerNow(
            ref context,
            pointer,
            block,
            (ref XFileReadContext pointerContext, ZonePointer<byte[]> p) =>
            {
                p.SetResult(pointerContext.ReadPointerValue(
                    p,
                    (ref XFileReadContext valueContext) =>
                        valueContext.ReadBytes(byteCount)));
            });
    }

    private static void ResolveRigidVertListArray(
        ref XFileReadContext context,
        ZonePointer<XRigidVertList[]> pointer,
        int count)
    {
        if (count <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(
            pointer,
            (ref XFileReadContext pointerContext, ZonePointer<XRigidVertList[]> p) =>
            {
                p.SetResult(pointerContext.ReadPointerValue(
                    p,
                    (ref XFileReadContext valueContext) =>
                    {
                        var values = new XRigidVertList[count];
                        for (var i = 0; i < values.Length; i++)
                            values[i] = ReadRigidVertList(ref valueContext);

                        return values;
                    }));

                if (p.Result is null)
                    return;

                foreach (var value in p.Result)
                    ResolveCollisionTree(ref pointerContext, value.CollisionTree);
            });
    }

    private static XRigidVertList ReadRigidVertList(ref XFileReadContext context)
    {
        var start = context.Position;
        var value = new XRigidVertList
        {
            BoneOffset = context.ReadUInt16(),
            VertCount = context.ReadUInt16(),
            TriOffset = context.ReadUInt16(),
            TriCount = context.ReadUInt16(),
            CollisionTree = context.ReadDirectPointer<XSurfaceCollisionTree>("XRigidVertList.CollisionTree"),
        };

        var bytesRead = context.Position - start;
        if (bytesRead != XRigidVertListSize)
            throw new InvalidDataException($"XRigidVertList read {bytesRead:N0} bytes; expected {XRigidVertListSize:N0} bytes.");

        return value;
    }

    private static void ResolveCollisionTree(
        ref XFileReadContext context,
        ZonePointer<XSurfaceCollisionTree> pointer)
    {
        if (!pointer.IsInlineData)
        {
            pointer.SetResult(default);
            return;
        }

        context.ResolveInlinePointerNow(
            pointer,
            (ref XFileReadContext pointerContext, ZonePointer<XSurfaceCollisionTree> p) =>
                p.SetResult(pointerContext.ReadPointerValue(p, ReadCollisionTree)));
    }

    private static XSurfaceCollisionTree ReadCollisionTree(ref XFileReadContext context)
    {
        var start = context.Position;
        var value = new XSurfaceCollisionTree
        {
            Trans = ReadVec3(ref context),
            Scale = ReadVec3(ref context),
            NodeCount = context.ReadUInt32(),
            Nodes = context.ReadDirectPointer<XSurfaceCollisionNode[]>("XSurfaceCollisionTree.Nodes"),
            LeafCount = context.ReadUInt32(),
            Leafs = context.ReadDirectPointer<XSurfaceCollisionLeaf[]>("XSurfaceCollisionTree.Leafs"),
        };

        var bytesRead = context.Position - start;
        if (bytesRead != XSurfaceCollisionTreeSize)
            throw new InvalidDataException($"XSurfaceCollisionTree read {bytesRead:N0} bytes; expected {XSurfaceCollisionTreeSize:N0} bytes.");

        ResolveCollisionNodeArray(ref context, value.Nodes, checked((int)value.NodeCount));
        ResolveCollisionLeafArray(ref context, value.Leafs, checked((int)value.LeafCount));

        return value;
    }

    private static void ResolveCollisionNodeArray(
        ref XFileReadContext context,
        ZonePointer<XSurfaceCollisionNode[]> pointer,
        int count)
    {
        if (count <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(
            pointer,
            (ref XFileReadContext pointerContext, ZonePointer<XSurfaceCollisionNode[]> p) =>
            {
                p.SetResult(pointerContext.ReadPointerValue(
                    p,
                    (ref XFileReadContext valueContext) =>
                    {
                        var values = new XSurfaceCollisionNode[count];
                        for (var i = 0; i < values.Length; i++)
                            values[i] = ReadCollisionNode(ref valueContext);

                        return values;
                    }));
            });
    }

    private static XSurfaceCollisionNode ReadCollisionNode(ref XFileReadContext context)
    {
        var start = context.Position;
        var value = new XSurfaceCollisionNode();
        for (var i = 0; i < value.Mins.Length; i++)
            value.Mins[i] = context.ReadUInt16();
        for (var i = 0; i < value.Maxs.Length; i++)
            value.Maxs[i] = context.ReadUInt16();

        value.ChildBeginIndex = context.ReadUInt16();
        value.ChildCount = context.ReadUInt16();

        var bytesRead = context.Position - start;
        if (bytesRead != XSurfaceCollisionNodeSize)
            throw new InvalidDataException($"XSurfaceCollisionNode read {bytesRead:N0} bytes; expected {XSurfaceCollisionNodeSize:N0} bytes.");

        return value;
    }

    private static void ResolveCollisionLeafArray(
        ref XFileReadContext context,
        ZonePointer<XSurfaceCollisionLeaf[]> pointer,
        int count)
    {
        if (count <= 0 || !pointer.IsInlineData)
        {
            pointer.SetResult([]);
            return;
        }

        context.ResolveInlinePointerNow(
            pointer,
            (ref XFileReadContext pointerContext, ZonePointer<XSurfaceCollisionLeaf[]> p) =>
            {
                p.SetResult(pointerContext.ReadPointerValue(
                    p,
                    (ref XFileReadContext valueContext) =>
                    {
                        var values = new XSurfaceCollisionLeaf[count];
                        for (var i = 0; i < values.Length; i++)
                            values[i] = ReadCollisionLeaf(ref valueContext);

                        return values;
                    }));
            });
    }

    private static XSurfaceCollisionLeaf ReadCollisionLeaf(ref XFileReadContext context)
    {
        var start = context.Position;
        var value = new XSurfaceCollisionLeaf
        {
            TriangleBeginIndex = context.ReadUInt16(),
        };

        var bytesRead = context.Position - start;
        if (bytesRead != XSurfaceCollisionLeafSize)
            throw new InvalidDataException($"XSurfaceCollisionLeaf read {bytesRead:N0} bytes; expected {XSurfaceCollisionLeafSize:N0} bytes.");

        return value;
    }

    private static void ResolveInlinePointerNow<T>(
        ref XFileReadContext context,
        ZonePointer<T> pointer,
        XFILE_BLOCK? block,
        XFilePointerResolver<T> resolver)
    {
        if (block is not { } materializationBlock)
        {
            context.ResolveInlinePointerNow(pointer, resolver);
            return;
        }

        context.PushStreamBlock(materializationBlock);
        try
        {
            context.ResolveInlinePointerNow(pointer, resolver);
        }
        finally
        {
            context.PopStreamBlock();
        }
    }

    private static Vec3 ReadVec3(ref XFileReadContext context)
    {
        return new Vec3
        {
            X = context.ReadFloat(),
            Y = context.ReadFloat(),
            Z = context.ReadFloat(),
        };
    }

    private static void TraceSpan(ref XFileReadContext context, string label, int start)
    {
        Trace(ref context, $"{label} src=0x{start:X8} end=0x{context.Position:X8} len=0x{context.Position - start:X}");
    }

    private static void Trace(ref XFileReadContext context, string message)
    {
        if (!TraceXSurfs || !IsTraceOffset(context.Position) || _traceXSurfsCount >= TraceXSurfsLimit)
            return;

        Interlocked.Increment(ref _traceXSurfsCount);
        var stream = context.GetActiveStreamAddress();
        Console.Error.WriteLine(
            $"[xsurf-trace] asset[{context.CurrentAssetIndex:D5}:{context.CurrentAssetType}] "
            + $"stream=b{stream.BlockIndex}:0x{stream.Offset:X8} {message}");
    }

    private static bool IsTraceOffset(int offset)
    {
        return offset >= TraceXSurfsStart && offset <= TraceXSurfsEnd;
    }

    private static bool IsTraceEnabled(string name)
    {
        return Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            && value != "0";
    }

    private static int GetTraceInt(string name, int fallback)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(value[2..], System.Globalization.NumberStyles.HexNumber, null, out var hex))
        {
            return hex;
        }

        return int.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }
}
