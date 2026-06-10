using System.Reflection;
using FastFile.Models.Assets;
using FastFile.Models.Assets.Menu.Elements;
using FastFile.Models.Data;
using FastFile.Models.Utils;
using FastFile.Models.Zone;
using FastFile.Models.Zone.Attributes;

namespace FastFile.Logic.Zone.Validation;

public static class XStructMetadataValidator
{
    public static XStructMetadataValidationResult ValidateAssetMetadata()
    {
        return ValidateAssembly(typeof(BaseAsset).Assembly);
    }

    public static XStructMetadataValidationResult ValidateAssembly(Assembly assembly)
    {
        var diagnostics = new List<XStructMetadataDiagnostic>();
        var types = assembly
            .GetTypes()
            .Where(type => type is { IsClass: true } or { IsValueType: true })
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        foreach (var type in types)
        {
            ValidateBaseAsset(type, diagnostics);
            ValidateXFields(type, diagnostics);
        }

        return new XStructMetadataValidationResult(diagnostics);
    }

    private static void ValidateBaseAsset(Type type, List<XStructMetadataDiagnostic> diagnostics)
    {
        if (!typeof(BaseAsset).IsAssignableFrom(type) || type == typeof(BaseAsset) || type.IsAbstract)
            return;

        if (type.GetCustomAttribute<XStructAttribute>() is null)
        {
            AddError(diagnostics, type, null, "BaseAsset subclass is missing XStructAttribute.");
        }
    }

    private static void ValidateXFields(Type type, List<XStructMetadataDiagnostic> diagnostics)
    {
        var fields = GetXFields(type);
        if (fields.Length == 0)
            return;

        var structAttr = type.GetCustomAttribute<XStructAttribute>();
        if (structAttr is null)
        {
            AddError(diagnostics, type, null, "Type declares XField properties but is missing XStructAttribute.");
            return;
        }

        if (structAttr.Size < 0)
            AddError(diagnostics, type, null, $"XStruct size 0x{structAttr.Size:X} is negative.");

        var ranges = new List<FieldRange>();
        foreach (var field in fields)
        {
            ValidateField(type, field.Property, field.Attribute, structAttr, diagnostics, ranges);
        }

        FieldRange? previous = null;
        foreach (var current in ranges.OrderBy(range => range.Start).ThenBy(range => range.End))
        {
            if (previous is not null && current.Start < previous.End)
            {
                AddError(
                    diagnostics,
                    type,
                    current.Property.Name,
                    $"Field range 0x{current.Start:X}-0x{current.End:X} overlaps {previous.Property.Name} " +
                    $"range 0x{previous.Start:X}-0x{previous.End:X}.");
            }

            previous = current;
        }
    }

    private static void ValidateField(
        Type ownerType,
        PropertyInfo property,
        XFieldAttribute field,
        XStructAttribute structAttr,
        List<XStructMetadataDiagnostic> diagnostics,
        List<FieldRange> ranges)
    {
        if (field.Offset < 0)
        {
            AddError(diagnostics, ownerType, property.Name, $"XField offset 0x{field.Offset:X} is negative.");
            return;
        }

        if (field.Count < 0)
            AddError(diagnostics, ownerType, property.Name, $"XField count {field.Count} is negative.");

        var pointerAttr = property.GetCustomAttribute<XPointerFieldAttribute>();
        var containsPointer = IsPointerLike(property.PropertyType);
        if (containsPointer && pointerAttr is null)
        {
            AddError(diagnostics, ownerType, property.Name, "Pointer field is missing XPointerFieldAttribute.");
        }
        else if (!containsPointer && pointerAttr is not null)
        {
            AddError(diagnostics, ownerType, property.Name, "XPointerFieldAttribute is only valid on XPointer fields.");
        }

        if (pointerAttr is not null)
            ValidatePointerField(ownerType, property, pointerAttr, diagnostics);

        if (!TryGetFieldSize(ownerType, property, field, diagnostics, out var size))
            return;

        var end = field.Offset + size;
        if (end > structAttr.Size)
        {
            AddError(
                diagnostics,
                ownerType,
                property.Name,
                $"Field range 0x{field.Offset:X}-0x{end:X} exceeds XStruct size 0x{structAttr.Size:X}.");
        }

        ranges.Add(new FieldRange(property, field.Offset, end));
    }

    private static void ValidatePointerField(
        Type ownerType,
        PropertyInfo property,
        XPointerFieldAttribute attr,
        List<XStructMetadataDiagnostic> diagnostics)
    {
        if (attr.ResolutionKind == PointerResolutionKind.Unknown)
            AddError(diagnostics, ownerType, property.Name, "Pointer resolution kind is Unknown.");

        var pointerTarget = GetPointerTargetType(property.PropertyType);
        if (pointerTarget is null)
            return;

        switch (attr.Target)
        {
            case XPointerTarget.None:
                break;

            case XPointerTarget.CString:
                if (Nullable.GetUnderlyingType(pointerTarget) is { } nullableTarget)
                    pointerTarget = nullableTarget;

                if (pointerTarget != typeof(string))
                {
                    AddError(
                        diagnostics,
                        ownerType,
                        property.Name,
                        $"CString pointer target must be string, but target is {FormatType(pointerTarget)}.");
                }

                break;

            case XPointerTarget.ByteArray:
                if (pointerTarget != typeof(byte[]))
                {
                    AddError(
                        diagnostics,
                        ownerType,
                        property.Name,
                        $"ByteArray pointer target must be byte[], but target is {FormatType(pointerTarget)}.");
                }

                ValidateCountMember(ownerType, property, attr, diagnostics);
                break;

            case XPointerTarget.Object:
                if (pointerTarget.IsArray)
                {
                    AddError(diagnostics, ownerType, property.Name, "Object pointer target cannot be an array.");
                }
                else if (!IsSupportedObjectPayload(pointerTarget))
                {
                    AddWarning(
                        diagnostics,
                        ownerType,
                        property.Name,
                        $"{FormatType(pointerTarget)} is not decorated with XStructAttribute; reader support must be custom.");
                }

                break;

            case XPointerTarget.ObjectArray:
                if (!pointerTarget.IsArray)
                {
                    AddError(diagnostics, ownerType, property.Name, "ObjectArray pointer target must be an array.");
                }
                else
                {
                    var arrayElementType = pointerTarget.GetElementType()!;
                    if (!IsSupportedArrayPayload(arrayElementType))
                    {
                        AddWarning(
                            diagnostics,
                            ownerType,
                            property.Name,
                            $"{FormatType(arrayElementType)} array elements are not handled by the generic reader.");
                    }
                }

                ValidateCountMember(ownerType, property, attr, diagnostics);
                break;

            case XPointerTarget.PointerArray:
                if (!pointerTarget.IsArray ||
                    pointerTarget.GetElementType() is not { } elementType ||
                    !IsXPointer(elementType))
                {
                    AddError(diagnostics, ownerType, property.Name, "PointerArray target must be an array of XPointer<T>.");
                }

                if (attr.ElementResolutionKind == PointerResolutionKind.Unknown)
                {
                    AddWarning(
                        diagnostics,
                        ownerType,
                        property.Name,
                        "PointerArray uses the root pointer resolution kind for its elements.");
                }

                if (attr.ElementTarget == XPointerTarget.None)
                {
                    AddWarning(
                        diagnostics,
                        ownerType,
                        property.Name,
                        "PointerArray elements are read but not materialized because ElementTarget is None.");
                }

                ValidateCountMember(ownerType, property, attr, diagnostics);
                break;

            default:
                AddError(diagnostics, ownerType, property.Name, $"Unsupported pointer target {attr.Target}.");
                break;
        }
    }

    private static void ValidateCountMember(
        Type ownerType,
        PropertyInfo property,
        XPointerFieldAttribute attr,
        List<XStructMetadataDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(attr.CountMember))
        {
            AddError(diagnostics, ownerType, property.Name, "Pointer target requires CountMember.");
            return;
        }

        var member = ownerType
            .GetMember(attr.CountMember, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault();

        if (member is null)
        {
            AddError(diagnostics, ownerType, property.Name, $"CountMember '{attr.CountMember}' does not exist.");
            return;
        }

        var memberType = member switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo countProperty => countProperty.PropertyType,
            _ => null
        };

        if (memberType is null || !typeof(IConvertible).IsAssignableFrom(memberType))
        {
            AddError(
                diagnostics,
                ownerType,
                property.Name,
                $"CountMember '{attr.CountMember}' must be convertible to int.");
        }
    }

    private static bool TryGetFieldSize(
        Type ownerType,
        PropertyInfo property,
        XFieldAttribute field,
        List<XStructMetadataDiagnostic> diagnostics,
        out int size)
    {
        size = 0;
        var propertyType = property.PropertyType;

        if (propertyType.IsArray)
        {
            var elementType = propertyType.GetElementType()!;
            if (!TryGetSingleValueSize(ownerType, property, elementType, diagnostics, out var elementSize))
                return false;

            if (!TryGetFixedArrayCount(ownerType, property, field, diagnostics, out var count))
                return false;

            size = checked(elementSize * count);
            return true;
        }

        if (!TryGetSingleValueSize(ownerType, property, propertyType, diagnostics, out size))
            return false;

        return true;
    }

    private static bool TryGetSingleValueSize(
        Type ownerType,
        PropertyInfo property,
        Type valueType,
        List<XStructMetadataDiagnostic> diagnostics,
        out int size)
    {
        var nonNullValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));

        size = nonNullValueType switch
        {
            { } when IsXPointer(nonNullValueType) => 4,
            { IsEnum: true } => GetPrimitiveSize(Enum.GetUnderlyingType(nonNullValueType)),
            { } when nonNullValueType == typeof(byte) || nonNullValueType == typeof(bool) => 1,
            { } when nonNullValueType == typeof(short) || nonNullValueType == typeof(ushort) => 2,
            { } when nonNullValueType == typeof(int) || nonNullValueType == typeof(uint) || nonNullValueType == typeof(float) => 4,
            { } when nonNullValueType == typeof(long) || nonNullValueType == typeof(ulong) || nonNullValueType == typeof(double) => 8,
            { } when nonNullValueType == typeof(Vec2) => 8,
            { } when nonNullValueType == typeof(Vec3) => 12,
            { } when nonNullValueType == typeof(Vec4) => 16,
            { } when nonNullValueType == typeof(Bounds) => 24,
            { } when nonNullValueType == typeof(ItemDefData) => 4,
            _ => -1
        };

        if (size >= 0)
            return true;

        if (nonNullValueType!.GetCustomAttribute<XStructAttribute>() is { } structAttr)
        {
            size = structAttr.Size;
            return true;
        }

        AddError(
            diagnostics,
            ownerType,
            property.Name,
            $"Unsupported XField type {FormatType(nonNullValueType!)}. Add XStructAttribute or explicit reader support.");

        return false;
    }

    private static int GetPrimitiveSize(Type type)
    {
        if (type == typeof(byte) || type == typeof(bool))
            return 1;

        if (type == typeof(short) || type == typeof(ushort))
            return 2;

        if (type == typeof(int) || type == typeof(uint) || type == typeof(float))
            return 4;

        if (type == typeof(long) || type == typeof(ulong) || type == typeof(double))
            return 8;

        return -1;
    }

    private static bool TryGetFixedArrayCount(
        Type ownerType,
        PropertyInfo property,
        XFieldAttribute field,
        List<XStructMetadataDiagnostic> diagnostics,
        out int count)
    {
        count = field.Count;
        if (count > 0)
            return true;

        try
        {
            var owner = Activator.CreateInstance(ownerType);
            if (owner is not null && property.GetValue(owner) is Array array)
            {
                count = array.Length;
                return true;
            }
        }
        catch (Exception ex) when (ex is MemberAccessException or MissingMethodException or TargetInvocationException)
        {
            AddError(
                diagnostics,
                ownerType,
                property.Name,
                $"Could not inspect default array length: {ex.Message}");
            return false;
        }

        AddError(diagnostics, ownerType, property.Name, "Array field requires XField.Count or a default initialized array.");
        return false;
    }

    private static bool IsSupportedObjectPayload(Type type)
    {
        return type.GetCustomAttribute<XStructAttribute>() is not null;
    }

    private static bool IsSupportedArrayPayload(Type elementType)
    {
        return elementType.GetCustomAttribute<XStructAttribute>() is not null ||
               IsXPointer(elementType) ||
               GetPrimitiveSize(elementType) > 0 ||
               elementType.IsEnum ||
               elementType == typeof(Vec2) ||
               elementType == typeof(Vec3) ||
               elementType == typeof(Vec4) ||
               elementType == typeof(Bounds);
    }

    private static bool IsPointerLike(Type type)
    {
        if (IsXPointer(type))
            return true;

        if (type.IsArray && type.GetElementType() is { } elementType)
            return IsPointerLike(elementType);

        return false;
    }

    private static bool IsXPointer(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(XPointer<>);
    }

    private static Type? GetPointerTargetType(Type type)
    {
        if (IsXPointer(type))
            return type.GetGenericArguments()[0];

        if (type.IsArray && type.GetElementType() is { } elementType)
            return GetPointerTargetType(elementType);

        return null;
    }

    private static XFieldMetadata[] GetXFields(Type type)
    {
        var fields = new List<XFieldMetadata>();
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var attr = property.GetCustomAttribute<XFieldAttribute>();
            if (attr is not null)
                fields.Add(new XFieldMetadata(property, attr));
        }

        return [..fields.OrderBy(field => field.Attribute.Offset)];
    }

    private static string FormatType(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        var name = type.Name[..type.Name.IndexOf('`')];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FormatType))}>";
    }

    private static void AddError(
        List<XStructMetadataDiagnostic> diagnostics,
        Type type,
        string? member,
        string message)
    {
        diagnostics.Add(new XStructMetadataDiagnostic(
            XStructMetadataSeverity.Error,
            FormatType(type),
            member,
            message));
    }

    private static void AddWarning(
        List<XStructMetadataDiagnostic> diagnostics,
        Type type,
        string? member,
        string message)
    {
        diagnostics.Add(new XStructMetadataDiagnostic(
            XStructMetadataSeverity.Warning,
            FormatType(type),
            member,
            message));
    }

    private sealed record XFieldMetadata(PropertyInfo Property, XFieldAttribute Attribute);

    private sealed record FieldRange(PropertyInfo Property, int Start, int End);
}
