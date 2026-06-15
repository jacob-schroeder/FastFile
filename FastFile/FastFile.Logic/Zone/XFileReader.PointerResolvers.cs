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
                ResolveObjectPointerDynamic(pointerObj, attr, owner);
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
        MaterializeCStringPointer(
            ptr,
            XPointerMaterializationPlan.AtBlockPosition(
                XPointerTarget.CString,
                attr.ResolutionKind,
                attr.PayloadBlock,
                alignment: attr.Alignment,
                offsetIsAliasCell: attr.OffsetIsAliasCell));
    }

    private void ResolveCurrentStreamObjectPointer<T>(XPointer<T> ptr)
        where T : class, new()
    {
        var materialization = MaterializePointer(
            ptr,
            XPointerMaterializationPlan.CurrentStream(XPointerTarget.Object, ptr.ResolutionKind));

        if (!materialization.ShouldReadPayload)
        {
            ptr.Value = materialization.Address is { } offsetAddress && TryGetCachedObject<T>(offsetAddress, out var cached)
                ? cached
                : null;
            return;
        }

        XBlockAddress address = materialization.Address
                                ?? throw new InvalidDataException("Current stream object pointer materialized without an address.");

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

        var materialization = MaterializePointer(
            ptr,
            attr.UseCurrentStream
                ? XPointerMaterializationPlan.CurrentStream(
                    XPointerTarget.ByteArray,
                    attr.ResolutionKind,
                    attr.Alignment,
                    offsetIsAliasCell: attr.OffsetIsAliasCell)
                : XPointerMaterializationPlan.AtBlockPosition(
                    XPointerTarget.ByteArray,
                    attr.ResolutionKind,
                    attr.PayloadBlock,
                    alignment: attr.Alignment,
                    offsetIsAliasCell: attr.OffsetIsAliasCell));

        if (materialization.IsNull)
        {
            ptr.Value = [];
            return;
        }

        if (!materialization.ShouldReadPayload)
        {
            ptr.Value = materialization.Address is { } address && TryGetCachedObject<byte[]>(address, out var cached)
                ? cached
                : [];

            if (ptr.Value.Length == 0 &&
                materialization.Address is { } emittedAddress &&
                TryGetEmittedBytes(emittedAddress, GetCount(owner, attr), out var emittedBytes))
            {
                ptr.Value = emittedBytes;
                CacheObject(emittedAddress, emittedBytes);
            }

            return;
        }

        int count = GetCount(owner, attr);
        if (count < 0)
            throw new InvalidDataException(
                $"Byte array pointer has negative count {count} from {DescribeCountOwner(owner, attr)}.");

        XBlockAddress payloadAddress = materialization.Address
                                       ?? throw new InvalidDataException("Byte array pointer materialized without an address.");

        WithStreamBlock(payloadAddress.Block, () =>
        {
            var address = payloadAddress;
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
        XPointerFieldAttribute attr,
        object owner)
    {
        var pointerType = pointerObj.GetType();
        var targetType = pointerType.GetGenericArguments()[0];
        dynamic ptr = pointerObj;

        if (attr.ResolutionKind == PointerResolutionKind.Unknown)
            throw new InvalidDataException($"{targetType.Name} pointer has unknown resolution semantics.");

        if (TryResolveAliasCellSentinelPointer(pointerObj, attr, owner, targetType))
            return;

        var materialization = MaterializePointer(
            ptr,
            CreateObjectMaterializationPlan(attr, targetType));

        if (!materialization.ShouldReadPayload)
        {
            object? aliasedObject = null;

            if (ShouldUseCachedOffsetReference(attr) &&
                ((XBlockAddress?)materialization.Address) is { } offsetAddress &&
                TryGetCachedObject(targetType, offsetAddress, out var cached))
            {
                ptr.Value = (dynamic)cached;
                CacheAliasCellObject(ptr.PatchAddress, cached);
            }
            else if (attr.OffsetIsAliasCell &&
                     TryGetCachedAliasedObject(targetType, ptr, out aliasedObject))
            {
                ptr.Value = (dynamic)aliasedObject!;
                CacheAliasCellObject(ptr.PatchAddress, aliasedObject!);
            }
            else
            {
                if (attr.OffsetIsAliasCell && ptr.Kind == PointerKind.Offset)
                {
                    // Some alias cells are forward insert targets that are only
                    // patched after a later asset materializes.
                    ptr.Address = null;
                    DeferObjectPointerResolution(pointerObj, attr, owner);
                }

                ptr.Value = null;
            }

            return;
        }

        XBlockAddress address = ((XBlockAddress?)materialization.Address)
                                ?? throw new InvalidDataException($"{targetType.Name} pointer materialized without an address.");

        WithStreamBlock(address.Block, () =>
        {
            if (TryGetCachedObject(targetType, address, out var cached))
            {
                ptr.Value = (dynamic)cached;
                CacheAliasCellObject(ptr.PatchAddress, cached);
                return;
            }

            SeekOrVerify(address.Offset);

            var value = Activator.CreateInstance(targetType)
                        ?? throw new InvalidDataException($"Could not create {targetType.Name}.");

            ReadObjectFields(value);
            CacheObject(address, value);
            CacheAliasCellObject(ptr.PatchAddress, value);
            ResolveLoadedObjectPointers(value);
            ptr.Value = (dynamic)value;
        });
    }

    private bool TryResolveAliasCellSentinelPointer(
        object pointerObj,
        XPointerFieldAttribute attr,
        object owner,
        Type targetType)
    {
        if (!attr.OffsetIsAliasCell ||
            pointerObj is not FastFile.Models.Zone.Pointer pointer ||
            pointer.Kind != PointerKind.Offset)
        {
            return false;
        }

        var aliasCellAddress = XPointerCodec.DecodeOffset(pointer.Raw);
        if (!TryReadEmittedInt32(aliasCellAddress, out int aliasRaw))
            return false;

        var aliasKind = XPointerCodec.GetKind(aliasRaw);
        if (aliasKind is not (PointerKind.Inline or PointerKind.Insert))
            return false;

        // Some wrapper offsets point to a second-stage pointer cell. If that cell
        // is still a sentinel, the engine loads through the cell and patches both
        // references to the same materialized object.
        var aliasPointer = CreateTypedPointer(
            targetType,
            aliasRaw,
            attr.ResolutionKind,
            aliasCellAddress);

        ResolvePointerDynamic(aliasPointer, attr, owner);

        var resolvedPointer = (FastFile.Models.Zone.Pointer)aliasPointer;
        var resolvedValue = GetPointerValue(aliasPointer);
        pointer.Address = resolvedPointer.Address;
        SetPointerValue(pointerObj, resolvedValue);

        if (pointer.PatchAddress is { } patchAddress &&
            pointer.Address is { } address)
        {
            _blocks.PatchPointerCell(patchAddress, address);
        }

        if (resolvedValue is { } value)
        {
            CacheAliasCellObject(aliasCellAddress, value);
            CacheAliasCellObject(pointer.PatchAddress, value);
        }

        return true;
    }

    private static object CreateTypedPointer(
        Type targetType,
        int raw,
        PointerResolutionKind resolutionKind,
        XBlockAddress patchAddress)
    {
        var method = typeof(XPointerCodec)
            .GetMethod(nameof(XPointerCodec.CreatePointer), BindingFlags.Public | BindingFlags.Static)!
            .MakeGenericMethod(targetType);

        return method.Invoke(null, [raw, resolutionKind, patchAddress])!;
    }

    private static object? GetPointerValue(object pointerObj)
    {
        return pointerObj.GetType()
            .GetProperty(nameof(XPointer<object>.Value))!
            .GetValue(pointerObj);
    }

    private static void SetPointerValue(
        object pointerObj,
        object? value)
    {
        pointerObj.GetType()
            .GetProperty(nameof(XPointer<object>.Value))!
            .SetValue(pointerObj, value);
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

        if (attr.ResolutionKind == PointerResolutionKind.Unknown)
            throw new InvalidDataException($"{elementType.Name} array pointer has unknown resolution semantics.");

        var plan = CreateArrayMaterializationPlan(
            XPointerTarget.ObjectArray,
            attr,
            GetArrayPayloadAlignment(elementType));
        var materialization = MaterializePointer(ptr, plan);

        if (!materialization.ShouldReadPayload)
        {
            if (ShouldUseCachedOffsetReference(attr) &&
                ((XBlockAddress?)materialization.Address) is { } offsetAddress &&
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

        int count = GetCount(owner, attr);
        if (count < 0)
            throw new InvalidDataException(
                $"{elementType.Name} object array pointer has negative count {count} from {DescribeCountOwner(owner, attr)} " +
                $"({DescribePointer(ptr)}).");

        XBlockAddress address = ((XBlockAddress?)materialization.Address)
                                ?? throw new InvalidDataException($"{elementType.Name} object array pointer materialized without an address.");

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

        if (attr.ResolutionKind == PointerResolutionKind.Unknown)
            throw new InvalidDataException($"{arrayType.Name} pointer array has unknown resolution semantics.");

        var plan = CreateArrayMaterializationPlan(
            XPointerTarget.PointerArray,
            attr,
            alignment: 4);
        var materialization = MaterializePointer(ptr, plan);

        if (!materialization.ShouldReadPayload)
        {
            if (ShouldUseCachedOffsetReference(attr) &&
                ((XBlockAddress?)materialization.Address) is { } offsetAddress &&
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

        int count = GetCount(owner, attr);
        if (count < 0)
            throw new InvalidDataException(
                $"{targetType.Name} pointer array has negative count {count} from {DescribeCountOwner(owner, attr)} " +
                $"({DescribePointer(ptr)}).");

        XBlockAddress address = ((XBlockAddress?)materialization.Address)
                                ?? throw new InvalidDataException($"{targetType.Name} pointer array materialized without an address.");

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

    private static XPointerMaterializationPlan CreateObjectMaterializationPlan(
        XPointerFieldAttribute attr,
        Type targetType)
    {
        if (attr.ResolutionKind == PointerResolutionKind.CurrentStream || attr.UseCurrentStream)
        {
            return XPointerMaterializationPlan.CurrentStream(
                XPointerTarget.Object,
                attr.ResolutionKind,
                attr.Alignment,
                offsetIsAliasCell: attr.OffsetIsAliasCell);
        }

        var payloadBlock = attr.ResolutionKind == PointerResolutionKind.Alias &&
                           typeof(BaseAsset).IsAssignableFrom(targetType)
            ? XFILE_BLOCK.TEMP
            : attr.PayloadBlock;

        return XPointerMaterializationPlan.AtBlockPosition(
            XPointerTarget.Object,
            attr.ResolutionKind,
            payloadBlock,
            offsetIsAliasCell: attr.OffsetIsAliasCell);
    }

    private static XPointerMaterializationPlan CreateArrayMaterializationPlan(
        XPointerTarget target,
        XPointerFieldAttribute attr,
        int alignment)
    {
        return attr.ResolutionKind == PointerResolutionKind.CurrentStream || attr.UseCurrentStream
            ? XPointerMaterializationPlan.CurrentStream(
                target,
                attr.ResolutionKind,
                attr.Alignment > 0 ? attr.Alignment : alignment,
                offsetIsAliasCell: attr.OffsetIsAliasCell)
            : XPointerMaterializationPlan.AllocatedBlock(
                target,
                attr.ResolutionKind,
                attr.PayloadBlock,
                attr.Alignment > 0 ? attr.Alignment : alignment,
                offsetIsAliasCell: attr.OffsetIsAliasCell);
    }

    private static bool ShouldUseCachedOffsetReference(XPointerFieldAttribute attr)
    {
        return attr.ResolutionKind != PointerResolutionKind.CurrentStream;
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
            $"{type.Name}.{prop.Name} {phase}: src=0x{_position:X} temp=0x{_blocks.GetPosition(XFILE_BLOCK.TEMP):X} " +
            $"large=0x{_blocks.GetPosition(XFILE_BLOCK.LARGE):X} ptr={DescribePointerValue(value)}");
    }

    private void TraceMenuResolve(string message)
    {
        if (Environment.GetEnvironmentVariable("FF_TRACE_MENU") != "1")
            return;

        Console.Error.WriteLine(
            $"{message}: src=0x{_position:X} temp=0x{_blocks.GetPosition(XFILE_BLOCK.TEMP):X} " +
            $"large=0x{_blocks.GetPosition(XFILE_BLOCK.LARGE):X}");
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
