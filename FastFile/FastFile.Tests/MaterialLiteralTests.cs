using System.Buffers.Binary;
using System.Reflection;
using FastFile.Logic.Assets.Readers;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Tests;

public sealed class MaterialLiteralTests
{
    [Fact]
    public void LoadLiteralFloat4ResolvesDeferredDirectOffsetPayload()
    {
        var raw = XPointer.EncodeOffset((int)XFILE_BLOCK.LARGE, 0x1234);
        var context = new DeferredLiteralContext(CreateFloat4Bytes(1, 2, 3, 4));
        var method = typeof(MaterialAssetReader).GetMethod(
            "Load_LiteralFloat4",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var literal = Assert.IsType<XPointer<float[]>>(
            method!.Invoke(null, [raw, context]));

        Assert.Equal(PointerKind.Offset, literal.Kind);
        Assert.Equal(XFILE_BLOCK.LARGE, literal.Address?.Block);
        Assert.Equal(0x1234, literal.Address?.Offset);
        Assert.NotNull(literal.Value);
        Assert.Equal(new[] { 1f, 2f, 3f, 4f }, literal.Value!);
    }

    private static byte[] CreateFloat4Bytes(
        float x,
        float y,
        float z,
        float w)
    {
        var bytes = new byte[16];
        BinaryPrimitives.WriteSingleBigEndian(bytes.AsSpan(0, 4), x);
        BinaryPrimitives.WriteSingleBigEndian(bytes.AsSpan(4, 4), y);
        BinaryPrimitives.WriteSingleBigEndian(bytes.AsSpan(8, 4), z);
        BinaryPrimitives.WriteSingleBigEndian(bytes.AsSpan(12, 4), w);
        return bytes;
    }

    private sealed class DeferredLiteralContext(byte[] emittedBytes) : IXAssetReaderContext
    {
        public int SourcePosition => 0;

        public XFILE_BLOCK ActiveStreamBlock => XFILE_BLOCK.LARGE;

        public int GetStreamPosition(XFILE_BLOCK block)
        {
            return 0;
        }

        public XPointer<T> ReadPointer<T>(PointerResolutionKind resolutionKind)
        {
            throw new NotSupportedException();
        }

        public XPointer<T> ReinterpretPointer<T>(
            XPointer<object> pointer,
            PointerResolutionKind resolutionKind)
        {
            throw new NotSupportedException();
        }

        public byte[] ReadCurrentStreamBytes(int count)
        {
            throw new NotSupportedException();
        }

        public T ReadCurrentStreamObject<T>()
            where T : class, new()
        {
            throw new NotSupportedException();
        }

        public void MaterializeCStringPointer(XPointer<string?> pointer)
        {
            throw new NotSupportedException();
        }

        public void ResolveSndAliasCustomName(XPointer<string> pointer)
        {
            throw new NotSupportedException();
        }

        public void ResolveObjectPointers(object value)
        {
            throw new NotSupportedException();
        }

        public void ResolveChildPointers(object? value)
        {
            throw new NotSupportedException();
        }

        public void ResolvePointerProperty(object owner, string propertyName)
        {
            throw new NotSupportedException();
        }

        public void ResolvePointerValue(
            object value,
            XPointerFieldAttribute attribute,
            object owner)
        {
            var pointer = Assert.IsType<XPointer<byte[]>>(value);
            pointer.Address = DecodeOffset(pointer.Raw);
        }

        public void ResolveCurrentStreamObjectPointer<T>(XPointer<T> pointer)
            where T : class, new()
        {
            throw new NotSupportedException();
        }

        public bool TryReadEmittedBytes(
            XBlockAddress address,
            int count,
            out byte[] value)
        {
            value = [];
            return false;
        }

        public void DeferEmittedBytes(
            XBlockAddress address,
            int count,
            Action<byte[]> onResolved)
        {
            Assert.Equal(XFILE_BLOCK.LARGE, address.Block);
            Assert.Equal(0x1234, address.Offset);
            Assert.Equal(16, count);
            onResolved(emittedBytes);
        }

        public void WithStreamBlock(XFILE_BLOCK block, Action action)
        {
            action();
        }

        private static XBlockAddress DecodeOffset(int raw)
        {
            var value = unchecked((uint)raw);
            var block = (XFILE_BLOCK)(value >> 28);
            var offset = checked((int)((value & 0x0FFFFFFF) - 1));
            return new XBlockAddress(block, offset);
        }
    }
}
