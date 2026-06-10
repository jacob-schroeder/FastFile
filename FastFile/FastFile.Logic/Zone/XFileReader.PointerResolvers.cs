using System.Reflection;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Data;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Zone;

public partial class XFileReader
{
    private void ResolveObjectPointers(object obj)
    {
        foreach (var handler in _assetReadHandlers)
        {
            if (handler.TryResolvePointers(obj, this))
                return;
        }

        var type = obj.GetType();

        foreach (var prop in GetOrderedXFields(type))
        {
            var value = prop.GetValue(obj);
            var attr = prop.GetCustomAttribute<XPointerFieldAttribute>();
            var traceWeapon = Environment.GetEnvironmentVariable("FF_TRACE_WEAPON") == "1" &&
                              type.Name == "WeaponDef";

            try
            {
                var fieldHandled = false;
                foreach (var handler in _assetReadHandlers)
                {
                    if (!handler.TryResolveField(obj, value, this))
                        continue;

                    fieldHandled = true;
                    break;
                }

                if (fieldHandled)
                    continue;
            }
            catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or TargetInvocationException or ArgumentOutOfRangeException)
            {
                throw new InvalidDataException(
                    $"Failed to resolve asset-specific field for {type.Name}.{prop.Name}.",
                    ex);
            }

            if (attr is not null)
            {
                if (value is not null)
                {
                    try
                    {
                        if (traceWeapon)
                            TraceWeaponResolve(type, prop, value, "begin");

                        ResolvePointerValueDynamic(value, attr, obj);

                        if (traceWeapon)
                            TraceWeaponResolve(type, prop, value, "end");
                    }
                    catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or TargetInvocationException or ArgumentOutOfRangeException)
                    {
                        throw new InvalidDataException(
                            $"Failed to resolve {type.Name}.{prop.Name} pointer " +
                            $"({attr.ResolutionKind}/{attr.Target}).",
                            ex);
                    }
                }

                continue;
            }

            try
            {
                ResolveChildPointers(value);
            }
            catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or TargetInvocationException or ArgumentOutOfRangeException)
            {
                throw new InvalidDataException(
                    $"Failed to resolve child pointers for {type.Name}.{prop.Name}.",
                    ex);
            }
        }
    }

    private void ResolveChildPointers(object? value)
    {
        switch (value)
        {
            case null:
                return;

            case Array array:
                foreach (var item in array)
                    ResolveChildPointers(item);
                return;
        }

        var type = value.GetType();
        if (type.GetCustomAttribute<XStructAttribute>() is not null)
            ResolveLoadedObjectPointers(value);
    }

    private void ResolveLoadedObjectPointers(object value)
    {
        foreach (var handler in _assetReadHandlers)
        {
            if (handler.TryResolveLoadedObjectPointers(value, this))
                return;
        }

        ResolveObjectPointers(value);
    }

    private void ResolvePointerValueDynamic(
        object value,
        XPointerFieldAttribute attr,
        object owner)
    {
        if (value is Array array)
        {
            foreach (var pointer in array)
            {
                if (pointer is not null)
                    ResolvePointerDynamic(pointer, attr, owner);
            }

            return;
        }

        ResolvePointerDynamic(value, attr, owner);
    }

    private void ResolvePointerDynamic(
        object pointerObj,
        XPointerFieldAttribute attr,
        object owner)
    {
        switch (attr.Target)
        {
            case XPointerTarget.None:
                return;

            case XPointerTarget.CString:
                ResolveCStringPointerDynamic(pointerObj, attr);
                break;

            case XPointerTarget.ByteArray:
                ResolveByteArrayPointerDynamic(pointerObj, attr, owner);
                break;

            case XPointerTarget.Object:
                ResolveObjectPointerDynamic(pointerObj, attr);
                break;

            case XPointerTarget.ObjectArray:
                ResolveObjectArrayPointerDynamic(pointerObj, attr, owner);
                break;

            case XPointerTarget.PointerArray:
                ResolvePointerArrayDynamic(pointerObj, attr, owner);
                break;

            default:
                throw new NotSupportedException($"Unsupported pointer target {attr.Target}.");
        }

        ReportAssetReadProgress();
    }

    private void ResolvePointerProperty(object owner, string propertyName)
    {
        var type = owner.GetType();
        var prop = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                   ?? throw new InvalidDataException($"{type.Name}.{propertyName} was not found.");
        var attr = prop.GetCustomAttribute<XPointerFieldAttribute>()
                   ?? throw new InvalidDataException($"{type.Name}.{propertyName} is missing XPointerFieldAttribute.");
        var value = prop.GetValue(owner);

        if (value is null)
            return;

        try
        {
            TraceMenuResolve($"{type.Name}.{propertyName} begin");
            ResolvePointerValueDynamic(value, attr, owner);
            TraceMenuResolve($"{type.Name}.{propertyName} end");
        }
        catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or TargetInvocationException or ArgumentOutOfRangeException)
        {
            throw new InvalidDataException(
                $"Failed to resolve {type.Name}.{propertyName} pointer ({attr.ResolutionKind}/{attr.Target}).",
                ex);
        }
    }

    private void ResolveCStringPointerDynamic(
        object pointerObj,
        XPointerFieldAttribute attr)
    {
        var ptr = (XPointer<string?>)pointerObj;
        WithStreamBlock(attr.PayloadBlock, () => MaterializeCStringPointer(ptr));
    }

    private void ResolveCurrentStreamObjectPointer<T>(XPointer<T> ptr)
        where T : class, new()
    {
        if (!TryMaterializeCurrentStreamPointer(ptr))
        {
            ptr.Value = ptr.Address is { } offsetAddress && TryGetCachedObject<T>(offsetAddress, out var cached)
                ? cached
                : null;
            return;
        }

        XBlockAddress address = ptr.Address!.Value;

        WithStreamBlock(address.Block, () =>
        {
            if (TryGetCachedObject<T>(address, out var cached))
            {
                ptr.Value = cached;
                return;
            }

            SeekOrVerify(address.Offset);

            var value = new T();
            ReadObjectFields(value);
            CacheObject(address, value);
            ResolveLoadedObjectPointers(value);
            ptr.Value = value;
        });
    }

    private void ResolveByteArrayPointerDynamic(
        object pointerObj,
        XPointerFieldAttribute attr,
        object owner)
    {
        var ptr = (XPointer<byte[]>)pointerObj;

        if (!TryMaterializePointer(
                ptr,
                () => new XBlockAddress(attr.PayloadBlock, _streamBlocks[(int)attr.PayloadBlock].Position)))
        {
            ptr.Value = [];
            return;
        }

        if (ptr.Kind == PointerKind.Offset)
        {
            ptr.Value = ptr.Address is { } address && TryGetCachedObject<byte[]>(address, out var cached)
                ? cached
                : [];
            return;
        }

        int count = GetCount(owner, attr);
        if (count < 0)
            throw new InvalidDataException(
                $"Byte array pointer has negative count {count} from {DescribeCountOwner(owner, attr)}.");

        WithStreamBlock(ptr.Address!.Value.Block, () =>
        {
            var address = ptr.Address.Value;
            if (TryGetCachedObject<byte[]>(address, out var cached))
            {
                ptr.Value = cached;
                return;
            }

            SeekOrVerify(address.Offset);
            ptr.Value = ReadBytes(count);
            CacheObject(address, ptr.Value);
        });
    }

    private void ResolveObjectPointerDynamic(
        object pointerObj,
        XPointerFieldAttribute attr)
    {
        var pointerType = pointerObj.GetType();
        var targetType = pointerType.GetGenericArguments()[0];
        dynamic ptr = pointerObj;
        Func<XBlockAddress> addressFactory =
            () =>
            {
                var block = attr.ResolutionKind == PointerResolutionKind.Alias &&
                            typeof(BaseAsset).IsAssignableFrom(targetType)
                    ? XFILE_BLOCK.TEMP
                    : attr.PayloadBlock;

                return new XBlockAddress(block, _streamBlocks[(int)block].Position);
            };

        if (attr.ResolutionKind == PointerResolutionKind.Unknown)
            throw new InvalidDataException($"{targetType.Name} pointer has unknown resolution semantics.");

        PointerKind pointerKind = ptr.Kind;
        if (attr.ResolutionKind == PointerResolutionKind.Alias &&
            pointerKind is not PointerKind.Inline and not PointerKind.Insert)
        {
            TryMaterializePointer(ptr, addressFactory);
            if (((XBlockAddress?)ptr.Address) is { } aliasAddress &&
                TryGetCachedObject(targetType, aliasAddress, out var cached))
            {
                ptr.Value = (dynamic)cached;
            }
            else
            {
                ptr.Value = null;
            }

            return;
        }

        if (attr.ResolutionKind != PointerResolutionKind.CurrentStream &&
            pointerKind == PointerKind.Offset)
        {
            TryMaterializePointer(ptr, addressFactory);
            if (((XBlockAddress?)ptr.Address) is { } offsetAddress &&
                TryGetCachedObject(targetType, offsetAddress, out var cached))
            {
                ptr.Value = (dynamic)cached;
            }
            else
            {
                ptr.Value = null;
            }

            return;
        }

        var materialized = attr.ResolutionKind == PointerResolutionKind.CurrentStream
            ? TryMaterializeCurrentStreamPointer(ptr)
            : TryMaterializePointer(ptr, addressFactory);

        if (!materialized)
        {
            ptr.Value = null;
            return;
        }

        XBlockAddress address = ((XBlockAddress?)ptr.Address)!.Value;

        WithStreamBlock(address.Block, () =>
        {
            if (TryGetCachedObject(targetType, address, out var cached))
            {
                ptr.Value = (dynamic)cached;
                return;
            }

            SeekOrVerify(address.Offset);

            var value = Activator.CreateInstance(targetType)
                        ?? throw new InvalidDataException($"Could not create {targetType.Name}.");

            ReadObjectFields(value);
            CacheObject(address, value);
            ResolveLoadedObjectPointers(value);
            ptr.Value = (dynamic)value;
        });
    }

    private void ResolveObjectArrayPointerDynamic(
        object pointerObj,
        XPointerFieldAttribute attr,
        object owner)
    {
        var pointerType = pointerObj.GetType();
        var arrayType = pointerType.GetGenericArguments()[0];
        var elementType = arrayType.GetElementType()
                          ?? throw new InvalidDataException($"{arrayType.Name} is not an array pointer target.");
        dynamic ptr = pointerObj;
        Func<XBlockAddress> addressFactory =
            () => AllocatePointerPayload(attr.PayloadBlock, GetArrayPayloadAlignment(elementType));

        if (attr.ResolutionKind == PointerResolutionKind.Unknown)
            throw new InvalidDataException($"{elementType.Name} array pointer has unknown resolution semantics.");

        var materialized = attr.ResolutionKind == PointerResolutionKind.CurrentStream
            ? TryMaterializeCurrentStreamPointer(ptr)
            : TryMaterializePointer(ptr, addressFactory);

        if (attr.ResolutionKind != PointerResolutionKind.CurrentStream &&
            ((PointerKind)ptr.Kind) == PointerKind.Offset)
        {
            if (((XBlockAddress?)ptr.Address) is { } offsetAddress &&
                TryGetCachedObject(arrayType, offsetAddress, out var cached))
            {
                ptr.Value = (dynamic)cached;
            }
            else
            {
                ptr.Value = (dynamic)Array.CreateInstance(elementType, 0);
            }

            return;
        }

        if (!materialized)
        {
            ptr.Value = (dynamic)Array.CreateInstance(elementType, 0);
            return;
        }

        int count = GetCount(owner, attr);
        if (count < 0)
            throw new InvalidDataException(
                $"{elementType.Name} object array pointer has negative count {count} from {DescribeCountOwner(owner, attr)} " +
                $"({DescribePointer(ptr)}).");

        XBlockAddress address = ((XBlockAddress?)ptr.Address)!.Value;

        WithStreamBlock(address.Block, () =>
        {
            if (TryGetCachedObject(arrayType, address, out var cached))
            {
                ptr.Value = (dynamic)cached;
                return;
            }

            SeekOrVerify(address.Offset);

            var values = Array.CreateInstance(elementType, count);
            for (var i = 0; i < count; i++)
            {
                object? value;
                if (elementType.GetCustomAttribute<XStructAttribute>() is not null)
                {
                    value = Activator.CreateInstance(elementType)
                            ?? throw new InvalidDataException($"Could not create {elementType.Name}.");

                    ReadObjectFields(value);
                    CacheObject(new XBlockAddress(address.Block, address.Offset + i * GetXStructSize(elementType)), value);
                }
                else
                {
                    value = ReadValue(elementType, null);
                }

                values.SetValue(value, i);
            }

            if (elementType.GetCustomAttribute<XStructAttribute>() is not null)
            {
                for (var i = 0; i < count; i++)
                {
                    if (values.GetValue(i) is { } value)
                        ResolveLoadedObjectPointers(value);
                }
            }

            ptr.Value = (dynamic)values;
            CacheObject(address, values);
        });
    }

    private void ResolvePointerArrayDynamic(
        object pointerObj,
        XPointerFieldAttribute attr,
        object owner)
    {
        var pointerType = pointerObj.GetType();
        var arrayType = pointerType.GetGenericArguments()[0];
        var elementType = arrayType.GetElementType()
                          ?? throw new InvalidDataException($"{arrayType.Name} is not an array pointer target.");

        if (!elementType.IsGenericType ||
            elementType.GetGenericTypeDefinition() != typeof(XPointer<>))
        {
            throw new InvalidDataException($"{arrayType.Name} is not an XPointer array.");
        }

        var targetType = elementType.GetGenericArguments()[0];
        var elementResolutionKind = attr.ElementResolutionKind == PointerResolutionKind.Unknown
            ? attr.ResolutionKind
            : attr.ElementResolutionKind;
        dynamic ptr = pointerObj;
        Func<XBlockAddress> addressFactory =
            () => AllocatePointerPayload(attr.PayloadBlock, 4);

        if (attr.ResolutionKind == PointerResolutionKind.Unknown)
            throw new InvalidDataException($"{arrayType.Name} pointer array has unknown resolution semantics.");

        var materialized = attr.ResolutionKind == PointerResolutionKind.CurrentStream
            ? TryMaterializeCurrentStreamPointer(ptr)
            : TryMaterializePointer(ptr, addressFactory);

        if (attr.ResolutionKind != PointerResolutionKind.CurrentStream &&
            ((PointerKind)ptr.Kind) == PointerKind.Offset)
        {
            if (((XBlockAddress?)ptr.Address) is { } offsetAddress &&
                TryGetCachedObject(arrayType, offsetAddress, out var cached))
            {
                ptr.Value = (dynamic)cached;
            }
            else
            {
                ptr.Value = (dynamic)Array.CreateInstance(elementType, 0);
            }

            return;
        }

        if (!materialized)
        {
            ptr.Value = (dynamic)Array.CreateInstance(elementType, 0);
            return;
        }

        int count = GetCount(owner, attr);
        if (count < 0)
            throw new InvalidDataException(
                $"{targetType.Name} pointer array has negative count {count} from {DescribeCountOwner(owner, attr)} " +
                $"({DescribePointer(ptr)}).");

        XBlockAddress address = ((XBlockAddress?)ptr.Address)!.Value;

        WithStreamBlock(address.Block, () =>
        {
            if (TryGetCachedObject(arrayType, address, out var cached))
            {
                ptr.Value = (dynamic)cached;
                return;
            }

            SeekOrVerify(address.Offset);

            var values = Array.CreateInstance(elementType, count);
            for (var i = 0; i < count; i++)
                values.SetValue(ReadPointerDynamic(targetType, elementResolutionKind), i);

            if (attr.ElementTarget != XPointerTarget.None)
            {
                var elementAttr = new XPointerFieldAttribute
                {
                    ResolutionKind = elementResolutionKind,
                    Target = attr.ElementTarget,
                    PayloadBlock = attr.PayloadBlock
                };

                for (var i = 0; i < count; i++)
                {
                    var pointer = values.GetValue(i);
                    if (pointer is null)
                        continue;

                    try
                    {
                        ResolvePointerDynamic(pointer, elementAttr, owner);
                    }
                    catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or TargetInvocationException or ArgumentOutOfRangeException)
                    {
                        throw new InvalidDataException(
                            $"Failed to resolve {targetType.Name} pointer array element {i} ({DescribePointer((FastFile.Models.Zone.Pointer)pointer)}).",
                            ex);
                    }
                }
            }

            ptr.Value = (dynamic)values;
            CacheObject(address, values);
        });
    }

    private static int GetCount(object owner, XPointerFieldAttribute attr)
    {
        if (attr.CountMember is null)
            throw new InvalidDataException("Pointer field requires CountMember.");

        var member = owner.GetType()
            .GetMember(attr.CountMember, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault()
            ?? throw new InvalidDataException($"Missing count member {attr.CountMember}.");

        object? value = member switch
        {
            PropertyInfo property => property.GetValue(owner),
            FieldInfo field => field.GetValue(field.IsStatic ? null : owner),
            _ => throw new InvalidDataException($"Count member {attr.CountMember} is not a field or property.")
        };

        if (value is null)
            throw new InvalidDataException($"Count member {attr.CountMember} was null.");

        if (value is not IConvertible convertible)
            throw new InvalidDataException($"Count member {attr.CountMember} is not convertible to int.");

        return convertible.ToInt32(null);
    }

    private XBlockAddress AllocatePointerPayload(XFILE_BLOCK block, int alignment)
    {
        var streamBlock = _streamBlocks[(int)block];
        streamBlock.Align(alignment);

        return streamBlock.Address;
    }

    private static int GetArrayPayloadAlignment(Type elementType)
    {
        if (elementType.GetCustomAttribute<XStructAttribute>() is not null)
            return 4;

        if (elementType.IsGenericType &&
            elementType.GetGenericTypeDefinition() == typeof(XPointer<>))
            return 4;

        if (elementType == typeof(byte) || elementType == typeof(bool))
            return 1;

        if (elementType == typeof(short) || elementType == typeof(ushort))
            return 2;

        return 4;
    }

    private static string DescribeCountOwner(object owner, XPointerFieldAttribute attr)
    {
        var ownerType = owner.GetType();
        var offset = ownerType.GetProperty(nameof(BaseAsset.Offset))
                     ?? ownerType.GetProperty("Offset");
        string extra = owner switch
        {
            ItemDef item => $", type {item.Type}, typeData 0x{item.TypeData?.Raw ?? 0:X8}",
            _ => string.Empty
        };

        if (offset is not null &&
            offset.PropertyType == typeof(int) &&
            offset.GetValue(owner) is int value)
        {
            return $"{ownerType.Name}@0x{value:X}.{attr.CountMember}{extra}";
        }

        return $"{ownerType.Name}.{attr.CountMember}{extra}";
    }

    private static string DescribePointer(FastFile.Models.Zone.Pointer pointer)
    {
        string address = pointer.Address is { } value
            ? $", address {value.Block}:0x{value.Offset:X}"
            : string.Empty;

        return $"pointer raw 0x{pointer.Raw:X8}, kind {pointer.Kind}{address}";
    }

    private void TraceWeaponResolve(Type type, PropertyInfo prop, object value, string phase)
    {
        Console.Error.WriteLine(
            $"{type.Name}.{prop.Name} {phase}: src=0x{_position:X} temp=0x{_streamBlocks[(int)XFILE_BLOCK.TEMP].Position:X} " +
            $"large=0x{_streamBlocks[(int)XFILE_BLOCK.LARGE].Position:X} ptr={DescribePointerValue(value)}");
    }

    private void TraceMenuResolve(string message)
    {
        if (Environment.GetEnvironmentVariable("FF_TRACE_MENU") != "1")
            return;

        Console.Error.WriteLine(
            $"{message}: src=0x{_position:X} temp=0x{_streamBlocks[(int)XFILE_BLOCK.TEMP].Position:X} " +
            $"large=0x{_streamBlocks[(int)XFILE_BLOCK.LARGE].Position:X}");
    }

    private static string DescribePointerValue(object value)
    {
        if (value is FastFile.Models.Zone.Pointer pointer)
            return DescribePointer(pointer);

        if (value is Array array)
        {
            var first = array.Cast<object?>().FirstOrDefault(item => item is FastFile.Models.Zone.Pointer);
            return first is FastFile.Models.Zone.Pointer firstPointer
                ? $"array[{array.Length}] first={DescribePointer(firstPointer)}"
                : $"array[{array.Length}]";
        }

        return value.GetType().Name;
    }
}
