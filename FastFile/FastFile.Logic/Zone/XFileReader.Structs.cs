using System.Buffers.Binary;
using System.Reflection;
using FastFile.Logic.Extensions;
using FastFile.Models.Assets;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Zone;

public partial class XFileReader
{
    private void ReadObjectFields(object obj)
    {
        var type = obj.GetType();
        var structAttr = type.GetCustomAttribute<XStructAttribute>();
        var rootOffset = _blocks.ActivePosition;
        var consumed = 0;

        SetOffsetIfPresent(obj, rootOffset);

        foreach (var prop in GetOrderedXFields(type))
        {
            var field = prop.GetCustomAttribute<XFieldAttribute>()!;

            if (field.Offset < consumed)
            {
                throw new InvalidDataException(
                    $"{type.Name}.{prop.Name} offset 0x{field.Offset:X} overlaps previously read data at 0x{consumed:X}.");
            }

            ReadBytes(field.Offset - consumed);

            try
            {
                prop.SetValue(obj, ReadFieldValue(obj, prop, field));
            }
            catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or TargetInvocationException)
            {
                throw new InvalidDataException(
                    $"Failed to read {type.Name}.{prop.Name} at {rootOffset:X8}+0x{field.Offset:X} " +
                    $"({ _blocks.ActiveBlockType } offset 0x{_blocks.ActivePosition:X}).",
                    ex);
            }

            consumed = _blocks.ActivePosition - rootOffset;
        }

        if (structAttr is null)
            return;

        if (consumed > structAttr.Size)
        {
            throw new InvalidDataException(
                $"{type.Name} consumed 0x{consumed:X} bytes but XStruct size is 0x{structAttr.Size:X}.");
        }

        ReadBytes(structAttr.Size - consumed);
    }

    private object? ReadFieldValue(
        object owner,
        PropertyInfo prop,
        XFieldAttribute field)
    {
        var pointerAttr = prop.GetCustomAttribute<XPointerFieldAttribute>();

        foreach (var handler in _assetReadHandlers)
        {
            if (handler.TryReadField(owner, prop, field, this, out var value))
                return value;
        }

        return prop.PropertyType.IsArray
            ? ReadArrayValue(owner, prop, field, pointerAttr)
            : ReadValue(prop.PropertyType, pointerAttr);
    }

    private object ReadArrayValue(
        object owner,
        PropertyInfo prop,
        XFieldAttribute field,
        XPointerFieldAttribute? pointerAttr)
    {
        var elementType = prop.PropertyType.GetElementType()
                          ?? throw new NotSupportedException($"Unsupported array type {prop.PropertyType.Name}.");
        var count = GetFixedArrayCount(owner, prop, field);
        var array = Array.CreateInstance(elementType, count);

        for (var i = 0; i < count; i++)
            array.SetValue(ReadValue(elementType, pointerAttr), i);

        return array;
    }

    private static int GetFixedArrayCount(
        object owner,
        PropertyInfo prop,
        XFieldAttribute field)
    {
        if (field.Count > 0)
            return field.Count;

        if (prop.GetValue(owner) is Array array)
            return array.Length;

        var defaultOwner = Activator.CreateInstance(owner.GetType());
        if (defaultOwner is not null && prop.GetValue(defaultOwner) is Array defaultArray)
            return defaultArray.Length;

        throw new InvalidDataException($"{owner.GetType().Name}.{prop.Name} needs an XField Count.");
    }

    private object? ReadValue(
        Type type,
        XPointerFieldAttribute? pointerAttr)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(XPointer<>))
        {
            if (pointerAttr is null)
                throw new InvalidDataException($"Pointer field {type.Name} is missing XPointerFieldAttribute.");

            var pointerTargetType = type.GetGenericArguments()[0];
            return ReadPointerDynamic(pointerTargetType, pointerAttr.ResolutionKind);
        }

        if (type.IsEnum)
        {
            var rawValue = ReadValue(Enum.GetUnderlyingType(type), null)
                           ?? throw new InvalidDataException($"Could not read enum {type.Name}.");
            return Enum.ToObject(type, rawValue);
        }

        if (type == typeof(byte))
            return ReadBytes(sizeof(byte))[0];

        if (type == typeof(bool))
        {
            var value = ReadBytes(sizeof(byte))[0];
            return value switch
            {
                0 => false,
                1 => true,
                _ => throw new InvalidDataException($"Invalid boolean value {value}.")
            };
        }

        if (type == typeof(short))
            return BinaryPrimitives.ReadInt16BigEndian(ReadBytes(sizeof(short)));

        if (type == typeof(ushort))
            return BinaryPrimitives.ReadUInt16BigEndian(ReadBytes(sizeof(ushort)));

        if (type == typeof(int))
            return ReadInt32();

        if (type == typeof(uint))
            return BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(sizeof(uint)));

        if (type == typeof(float))
            return BinaryPrimitives.ReadSingleBigEndian(ReadBytes(sizeof(float)));

        if (type == typeof(ulong))
            return BinaryPrimitives.ReadUInt64BigEndian(ReadBytes(sizeof(ulong)));

        if (type == typeof(Vec2))
        {
            return new Vec2
            {
                a = (float)ReadValue(typeof(float), null)!,
                b = (float)ReadValue(typeof(float), null)!
            };
        }

        if (type == typeof(Vec3))
        {
            return new Vec3
            {
                X = (float)ReadValue(typeof(float), null)!,
                Y = (float)ReadValue(typeof(float), null)!,
                Z = (float)ReadValue(typeof(float), null)!
            };
        }

        if (type == typeof(Vec4))
        {
            return new Vec4
            {
                A = (float)ReadValue(typeof(float), null)!,
                R = (float)ReadValue(typeof(float), null)!,
                G = (float)ReadValue(typeof(float), null)!,
                B = (float)ReadValue(typeof(float), null)!
            };
        }

        if (type == typeof(Bounds))
        {
            return new Bounds
            {
                MidPoint = (Vec3)ReadValue(typeof(Vec3), null)!,
                HalfSize = (Vec3)ReadValue(typeof(Vec3), null)!
            };
        }

        if (type.GetCustomAttribute<XStructAttribute>() is not null)
        {
            var value = Activator.CreateInstance(type)
                        ?? throw new InvalidDataException($"Could not create {type.Name}.");
            ReadObjectFields(value);
            return value;
        }

        throw new NotSupportedException($"Unsupported field type {type.Name}.");
    }

    private static void SetOffsetIfPresent(object obj, int offset)
    {
        var prop = obj.GetType().GetProperty(nameof(BaseAsset.Offset))
                   ?? obj.GetType().GetProperty("Offset");

        if (prop is null || !prop.CanWrite || prop.PropertyType != typeof(int))
            return;

        prop.SetValue(obj, offset);
    }

    private object ReadPointerDynamic(
        Type targetType,
        PointerResolutionKind resolutionKind)
    {
        var method = typeof(XFileReader)
            .GetMethod(nameof(ReadPointer), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(targetType);

        return method.Invoke(this, [resolutionKind, true])!;
    }

    private static IEnumerable<PropertyInfo> GetOrderedXFields(Type type)
    {
        return type.GetProperties()
            .Select(p => new
            {
                Property = p,
                Field = p.GetCustomAttribute<XFieldAttribute>()
            })
            .Where(x => x.Field is not null)
            .OrderBy(x => x.Field!.Offset)
            .Select(x => x.Property);
    }

    private int ReadInt32ToActiveBlock(int blockOffset)
    {
        int value = Span.ReadInt32(ref _position);
        _blocks.PatchInt32(new XBlockAddress(_blocks.ActiveBlockType, blockOffset), value);
        ReportAssetReadProgress();
        return value;
    }

    private XPointer<T> ReadPointerToActiveBlock<T>(
        int blockOffset,
        PointerResolutionKind resolutionKind)
    {
        int raw = ReadInt32ToActiveBlock(blockOffset);

        return CreatePointerFromPatchedRaw<T>(raw, blockOffset, resolutionKind);
    }

    private XPointer<T> CreatePointerFromPatchedRaw<T>(
        int raw,
        int blockOffset,
        PointerResolutionKind resolutionKind)
    {
        return XPointerCodec.CreatePointer<T>(
            raw,
            resolutionKind,
            new XBlockAddress(_blocks.ActiveBlockType, blockOffset));
    }

    private static XPointer<T> ReinterpretPointer<T>(
        XPointer<object> ptr,
        PointerResolutionKind resolutionKind)
    {
        return XPointerCodec.ReinterpretPointer<T>(ptr, resolutionKind);
    }

    private static int GetXStructSize(Type type)
    {
        return type.GetCustomAttribute<XStructAttribute>()?.Size
               ?? throw new InvalidDataException($"{type.Name} is missing XStructAttribute.");
    }

    private static XFILE_BLOCK GetXStructBlock(Type type)
    {
        return type.GetCustomAttribute<XStructAttribute>()?.Block
               ?? throw new InvalidDataException($"{type.Name} is missing XStructAttribute.");
    }
}
