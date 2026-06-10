using System.Reflection;
using FastFile.Logic.Extensions;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Localize;
using FastFile.Models.Assets.TechniqueSet;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Zone;

public partial class XFileReader
{
    private void ReadObjectFields(object obj)
    {
        var type = obj.GetType();

        foreach (var prop in GetOrderedXFields(type))
        {
            if (prop.PropertyType == typeof(int))
            {
                prop.SetValue(obj, ReadInt32());
                continue;
            }

            if (prop.PropertyType == typeof(uint))
            {
                prop.SetValue(obj, unchecked((uint)ReadInt32()));
                continue;
            }

            if (prop.PropertyType.IsGenericType &&
                prop.PropertyType.GetGenericTypeDefinition() == typeof(XPointer<>))
            {
                var pointerTargetType = prop.PropertyType.GetGenericArguments()[0];

                var attr = prop.GetCustomAttribute<XPointerFieldAttribute>()
                           ?? throw new InvalidDataException($"{type.Name}.{prop.Name} is missing XPointerFieldAttribute.");

                var pointer = ReadPointerDynamic(pointerTargetType, attr.ResolutionKind, emit: true);
                prop.SetValue(obj, pointer);
                continue;
            }

            throw new NotSupportedException(
                $"Unsupported field type {prop.PropertyType.Name} on {type.Name}.{prop.Name}.");
        }
    }
    
    private object ReadPointerDynamic(
        Type targetType,
        PointerResolutionKind resolutionKind,
        bool emit)
    {
        var method = typeof(XFileReader)
            .GetMethod(nameof(ReadPointer), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(targetType);

        return method.Invoke(this, [resolutionKind, emit])!;
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
    
    private void ResolveObjectPointers(object obj)
    {
        var type = obj.GetType();

        foreach (var prop in GetOrderedXFields(type))
        {
            var attr = prop.GetCustomAttribute<XPointerFieldAttribute>();
            if (attr is null)
                continue;

            var pointer = prop.GetValue(obj);
            if (pointer is null)
                continue;

            ResolvePointerDynamic(pointer, attr, obj);
        }
    }
    
    private void ResolvePointerDynamic(
        object pointerObj,
        XPointerFieldAttribute attr,
        object owner)
    {
        switch (attr.Target)
        {
            case XPointerTarget.CString:
                ResolveCStringPointerDynamic(pointerObj);
                break;

            case XPointerTarget.ByteArray:
                ResolveByteArrayPointerDynamic(pointerObj, attr, owner);
                break;

            case XPointerTarget.Object:
                throw new NotSupportedException();
                //ResolveObjectPointerDynamic(pointerObj, attr);
                break;

            case XPointerTarget.PointerArray:
                throw new NotSupportedException();
                //ResolvePointerArrayDynamic(pointerObj, attr, owner);
                break;

            default:
                throw new NotSupportedException($"Unsupported pointer target {attr.Target}.");
        }
    }
    
    private void ResolveCStringPointerDynamic(object pointerObj)
    {
        var ptr = (XPointer<string?>)pointerObj;
        MaterializeCStringPointer(ptr);
    }
    
    private void ResolveByteArrayPointerDynamic(
        object pointerObj,
        XPointerFieldAttribute attr,
        object owner)
    {
        var ptr = (XPointer<byte[]>)pointerObj;

        int count = GetCount(owner, attr);

        if (!TryMaterializePointer(
                ptr,
                () => new XBlockAddress(attr.PayloadBlock, _streamBlocks[(int)attr.PayloadBlock].Position)))
        {
            ptr.Value = [];
            return;
        }

        WithStreamBlock(ptr.Address!.Value.Block, () =>
        {
            SeekOrVerify(ptr.Address.Value.Offset);

            byte[] data = Span.Slice(_position, count).ToArray();
            _position += count;

            _activeBlock.Write(data);
            ptr.Value = data;
        });
    }
    
    
    
    private static int GetCount(object owner, XPointerFieldAttribute attr)
    {
        if (attr.CountMember is null)
            throw new InvalidDataException("Pointer field requires CountMember.");

        var prop = owner.GetType().GetProperty(attr.CountMember)
                   ?? throw new InvalidDataException($"Missing count member {attr.CountMember}.");

        return (int)(prop.GetValue(owner)
                     ?? throw new InvalidDataException($"Count member {attr.CountMember} was null."));
    }
}