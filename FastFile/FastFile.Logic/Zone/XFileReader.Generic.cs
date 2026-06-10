using System.Buffers.Binary;
using System.Reflection;
using FastFile.Logic.Extensions;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menu.Enums;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Assets.XModels;
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
        var rootOffset = _activeBlock.Position;
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
                    $"({ _activeBlock.BlockType } offset 0x{_activeBlock.Position:X}).",
                    ex);
            }

            consumed = _activeBlock.Position - rootOffset;
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

        if (owner is ItemDef item && prop.PropertyType == typeof(ItemDefData))
            return ReadItemDefTypeDataPointer(item);

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

    private void ResolveObjectPointers(object obj)
    {
        if (obj is MenuDef menu)
        {
            ResolveMenuDefPointers(menu);
            return;
        }

        if (obj is MenuEventHandler handler)
        {
            ResolveMenuEventHandler(handler);
            return;
        }

        if (obj is ConditionalScript conditionalScript)
        {
            ResolveConditionalScript(conditionalScript);
            return;
        }

        if (obj is XSurface surface)
        {
            ResolveXSurfacePointers(surface);
            return;
        }

        if (obj is ExpressionEntry entry)
        {
            ResolveExpressionEntry(entry);
            return;
        }

        if (obj is EventData)
            return;

        if (obj is OperandInternalData)
            return;

        var type = obj.GetType();

        foreach (var prop in GetOrderedXFields(type))
        {
            var value = prop.GetValue(obj);
            var attr = prop.GetCustomAttribute<XPointerFieldAttribute>();
            var traceWeapon = Environment.GetEnvironmentVariable("FF_TRACE_WEAPON") == "1" &&
                              type.Name == "WeaponDef";

            if (obj is ItemDef item && value is ItemDefData typeData)
            {
                try
                {
                    ResolveItemDefTypeData(item, typeData);
                }
                catch (Exception ex) when (ex is InvalidDataException or NotSupportedException or TargetInvocationException or ArgumentOutOfRangeException)
                {
                    throw new InvalidDataException(
                        $"Failed to resolve ItemDef.TypeData pointer for item type {item.Type}.",
                        ex);
                }

                continue;
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

    private void TraceWeaponResolve(Type type, PropertyInfo prop, object value, string phase)
    {
        Console.Error.WriteLine(
            $"{type.Name}.{prop.Name} {phase}: src=0x{_position:X} temp=0x{_streamBlocks[(int)XFILE_BLOCK.TEMP].Position:X} " +
            $"large=0x{_streamBlocks[(int)XFILE_BLOCK.LARGE].Position:X} ptr={DescribePointerValue(value)}");
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
        if (value is MenuDef)
        {
            WithStreamBlock(XFILE_BLOCK.LARGE, () => ResolveObjectPointers(value));
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

    private void ResolveMenuDefPointers(MenuDef menu)
    {
        ResolvePointerProperty(menu, nameof(MenuDef.ExpressionData));
        ResolveChildPointers(menu.Window);
        ResolvePointerProperty(menu, nameof(MenuDef.FontPtr));
        ResolvePointerProperty(menu, nameof(MenuDef.OnOpen));
        ResolvePointerProperty(menu, nameof(MenuDef.OnClose));
        ResolvePointerProperty(menu, nameof(MenuDef.OnRequestClose));
        ResolvePointerProperty(menu, nameof(MenuDef.OnEsc));
        ResolvePointerProperty(menu, nameof(MenuDef.ExecKeys));
        ResolvePointerProperty(menu, nameof(MenuDef.VisibleExp));
        ResolvePointerProperty(menu, nameof(MenuDef.AllowedBinding));
        ResolvePointerProperty(menu, nameof(MenuDef.SoundName));
        ResolvePointerProperty(menu, nameof(MenuDef.RectXExp));
        ResolvePointerProperty(menu, nameof(MenuDef.RectYExp));
        ResolvePointerProperty(menu, nameof(MenuDef.RectWExp));
        ResolvePointerProperty(menu, nameof(MenuDef.RectHExp));
        ResolvePointerProperty(menu, nameof(MenuDef.Items));
    }

    private void ResolveMenuEventHandler(MenuEventHandler handler)
    {
        var data = handler.EventData
                   ?? throw new InvalidDataException("MenuEventHandler.EventData was null.");
        var pointer = data.DataPtr
                      ?? throw new InvalidDataException("MenuEventHandler.EventData.DataPtr was null.");

        switch (handler.EventType)
        {
            case 0:
                data.UnconditionalScript = ReinterpretPointer<string?>(pointer, PointerResolutionKind.Direct);
                MaterializeCStringPointer(data.UnconditionalScript);
                break;

            case 1:
                data.ConditionalScript = ReinterpretPointer<ConditionalScript>(pointer, PointerResolutionKind.CurrentStream);
                ResolveCurrentStreamObjectPointer(data.ConditionalScript);
                break;

            case 2:
                data.ElseScript = ReinterpretPointer<MenuEventHandlerSet>(pointer, PointerResolutionKind.CurrentStream);
                ResolveCurrentStreamObjectPointer(data.ElseScript);
                break;

            case >= 3 and <= 6:
                data.SetLocalVarData = ReinterpretPointer<SetLocalVarData>(pointer, PointerResolutionKind.CurrentStream);
                ResolveCurrentStreamObjectPointer(data.SetLocalVarData);
                break;
        }
    }

    private void ResolveConditionalScript(ConditionalScript conditionalScript)
    {
        // EBOOT 0x10c028 resolves +0x04 before +0x00.
        ResolvePointerProperty(conditionalScript, nameof(ConditionalScript.EventExpression));
        ResolvePointerProperty(conditionalScript, nameof(ConditionalScript.EventHandlerSet));
    }

    private void ResolveXSurfacePointers(XSurface surface)
    {
        // EBOOT 0xf8400/0xf8628/0xf8838 keep the inline payload in the
        // current stream when the matching stream flag is set. Otherwise these
        // RSX-read surface buffers live in the PS3 vertex stream block.
        ResolvePointerValueDynamic(
            surface.TriIndices,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ObjectArray,
                CountMember = nameof(XSurface.TriIndexCount),
                PayloadBlock = surface.TriIndicesInCurrentBlock ? XFILE_BLOCK.LARGE : XFILE_BLOCK.XFILE_BLOCK_VERTEX
            },
            surface);

        ResolvePointerValueDynamic(
            surface.VertInfo.VertsBlend,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ObjectArray,
                CountMember = nameof(XSurfaceVertexInfo.BlendVertCount),
                PayloadBlock = XFILE_BLOCK.LARGE
            },
            surface.VertInfo);

        ResolvePointerValueDynamic(
            surface.Verts0,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ByteArray,
                CountMember = nameof(XSurface.VertexByteCount),
                PayloadBlock = surface.Verts0InCurrentBlock ? XFILE_BLOCK.LARGE : XFILE_BLOCK.XFILE_BLOCK_VERTEX
            },
            surface);

        ResolvePointerValueDynamic(
            surface.Verts1,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ByteArray,
                CountMember = nameof(XSurface.VertexByteCount),
                PayloadBlock = surface.Verts1InCurrentBlock ? XFILE_BLOCK.LARGE : XFILE_BLOCK.XFILE_BLOCK_VERTEX
            },
            surface);

        ResolvePointerValueDynamic(
            surface.VertList,
            new XPointerFieldAttribute
            {
                ResolutionKind = PointerResolutionKind.Direct,
                Target = XPointerTarget.ObjectArray,
                CountMember = nameof(XSurface.VertListCount),
                PayloadBlock = XFILE_BLOCK.LARGE
            },
            surface);
    }

    private void ResolveExpressionEntry(ExpressionEntry entry)
    {
        if (entry.Type == 0)
            return;

        var operand = entry.Data?.Operand
                      ?? throw new InvalidDataException("ExpressionEntry.Data.Operand was null.");
        var internals = operand.Internals
                        ?? throw new InvalidDataException("ExpressionEntry operand internals were null.");

        var payload = internals.DataPtr
                      ?? throw new InvalidDataException("ExpressionEntry operand payload was null.");

        switch (operand.DataType)
        {
            case ExpDataType.VAL_INT:
            case ExpDataType.VAL_FLOAT:
                break;

            case ExpDataType.VAL_STRING:
                internals.StringVal = ReinterpretPointer<string?>(payload, PointerResolutionKind.Direct);
                MaterializeCStringPointer(internals.StringVal);
                break;

            case ExpDataType.VAL_FUNCTION:
                internals.Function = ReinterpretPointer<Statement>(payload, PointerResolutionKind.CurrentStream);
                ResolveCurrentStreamObjectPointer(internals.Function);
                break;
        }
    }

    private int ReadInt32ToActiveBlock(int blockOffset)
    {
        int value = Span.ReadInt32(ref _position);
        _activeBlock.PatchInt32(blockOffset, value);
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
        return new XPointer<T>
        {
            Raw = raw,
            Kind = raw switch
            {
                0 => PointerKind.Null,
                -1 => PointerKind.Inline,
                -2 => PointerKind.Insert,
                _ => PointerKind.Offset
            },
            ResolutionKind = resolutionKind,
            PatchAddress = new XBlockAddress(_activeBlock.BlockType, blockOffset)
        };
    }

    private static XPointer<T> ReinterpretPointer<T>(
        XPointer<object> ptr,
        PointerResolutionKind resolutionKind)
    {
        return new XPointer<T>
        {
            Raw = ptr.Raw,
            Kind = ptr.Kind,
            ResolutionKind = resolutionKind,
            PatchAddress = ptr.PatchAddress,
            Address = ptr.Address
        };
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

    private void TraceMenuResolve(string message)
    {
        if (Environment.GetEnvironmentVariable("FF_TRACE_MENU") != "1")
            return;

        Console.Error.WriteLine(
            $"{message}: src=0x{_position:X} temp=0x{_streamBlocks[(int)XFILE_BLOCK.TEMP].Position:X} " +
            $"large=0x{_streamBlocks[(int)XFILE_BLOCK.LARGE].Position:X}");
    }

    private void ResolveCStringPointerDynamic(
        object pointerObj,
        XPointerFieldAttribute attr)
    {
        var ptr = (XPointer<string?>)pointerObj;
        WithStreamBlock(attr.PayloadBlock, () => MaterializeCStringPointer(ptr));
    }

    private ItemDefData ReadItemDefTypeDataPointer(ItemDef item)
    {
        var typeData = new ItemDefData();

        switch (item.Type)
        {
            case 6:
                typeData.ListBox = ReadPointer<ListBoxDef>(PointerResolutionKind.CurrentStream);
                typeData.Raw = typeData.ListBox.Raw;
                break;

            case 12:
                typeData.Multi = ReadPointer<MultiDef>(PointerResolutionKind.CurrentStream);
                typeData.Raw = typeData.Multi.Raw;
                break;

            case 13:
                typeData.EnumDvarName = ReadPointer<string?>(PointerResolutionKind.Direct);
                typeData.Raw = typeData.EnumDvarName.Raw;
                break;

            case 20:
                typeData.NewsTicker = ReadPointer<NewsTickerDef>(PointerResolutionKind.CurrentStream);
                typeData.Raw = typeData.NewsTicker.Raw;
                break;

            case 21:
                typeData.TextScroll = ReadPointer<TextScrollDef>(PointerResolutionKind.CurrentStream);
                typeData.Raw = typeData.TextScroll.Raw;
                break;

            case 0:
            case 4:
            case 9:
            case 10:
            case 11:
            case 14:
            case 16:
            case 17:
            case 18:
            case 22:
            case 23:
                typeData.EditField = ReadPointer<EditFieldDef>(PointerResolutionKind.CurrentStream);
                typeData.Raw = typeData.EditField.Raw;
                break;

            default:
                typeData.Raw = ReadPointer<ItemDefData>(PointerResolutionKind.CurrentStream).Raw;
                break;
        }

        return typeData;
    }

    private void ResolveItemDefTypeData(ItemDef item, ItemDefData typeData)
    {
        if (typeData.ListBox is not null)
        {
            ResolveCurrentStreamObjectPointer(typeData.ListBox);
            return;
        }

        if (typeData.Multi is not null)
        {
            ResolveCurrentStreamObjectPointer(typeData.Multi);
            return;
        }

        if (typeData.EnumDvarName is not null)
        {
            MaterializeCStringPointer(typeData.EnumDvarName);
            return;
        }

        if (typeData.NewsTicker is not null)
        {
            ResolveCurrentStreamObjectPointer(typeData.NewsTicker);
            return;
        }

        if (typeData.TextScroll is not null)
        {
            ResolveCurrentStreamObjectPointer(typeData.TextScroll);
            return;
        }

        if (typeData.EditField is not null)
            ResolveCurrentStreamObjectPointer(typeData.EditField);
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

    private static int GetXStructSize(Type type)
    {
        return type.GetCustomAttribute<XStructAttribute>()?.Size
               ?? throw new InvalidDataException($"{type.Name} is missing XStructAttribute.");
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

    private static XFILE_BLOCK GetXStructBlock(Type type)
    {
        return type.GetCustomAttribute<XStructAttribute>()?.Block
               ?? throw new InvalidDataException($"{type.Name} is missing XStructAttribute.");
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
}
