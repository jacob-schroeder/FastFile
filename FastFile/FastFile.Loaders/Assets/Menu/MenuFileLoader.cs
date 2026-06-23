using FastFile.Models.Assets.Menu;
using FastFile.Models.Math;
using FastFile.Models.Pointers;
using FastFile.Models.Zone;
using FastFile.Runtime;
using FastFile.Runtime.IO;

namespace FastFile.Loaders.Assets.Menu;

public sealed class MenuFileLoader
{
    private const int MenuFileSize = 0x0c;
    private const int MenuDefSize = MenuDefAsset.SerializedSize;

    public MenuFileAsset LoadFromAssetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context,
        out string? stopReason)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
            throw new InvalidDataException($"Top-level MenuFile pointer 0x{pointer.Raw:X8} does not reference inline payload data.");

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.Blocks.AlignCurrent(4);
            return ReadMenuFile(cursor, context, out stopReason);
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static MenuFileAsset ReadMenuFile(
        FastFileCursor cursor,
        FastFileLoadContext context,
        out string? stopReason)
    {
        stopReason = null;
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, MenuFileSize);
        var rootCursor = new FastFileCursor(rootBytes);

        int namePointer = rootCursor.ReadInt32();
        int menuCount = rootCursor.ReadInt32();
        int menusPointer = rootCursor.ReadInt32();

        if (rootCursor.Offset != MenuFileSize)
            throw new InvalidDataException($"MenuFile consumed 0x{rootCursor.Offset:X} bytes instead of 0x{MenuFileSize:X}.");

        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            string? name = ReadXString(cursor, namePointer, context);
            XPointerReference menusPointerRef = context.PointerReader.FromRaw(menusPointer, XPointerOffsetMode.Direct);
            IReadOnlyList<MenuDefReference> menus = ReadMenuDefPointerArray(
                cursor,
                menusPointerRef,
                menuCount,
                context,
                out stopReason);

            return new MenuFileAsset
            {
                Offset = offset,
                NamePointer = context.PointerReader.FromRaw<string>(namePointer, XPointerResolutionMode.Direct),
                Name = name,
                MenuCount = menuCount,
                MenusPointer = menusPointerRef.AsPointer<XPointer<MenuDefAsset>[]>(),
                Menus = menus
            };
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static IReadOnlyList<MenuDefReference> ReadMenuDefPointerArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context,
        out string? stopReason)
    {
        stopReason = null;

        if (count < 0)
            throw new InvalidDataException($"Invalid negative MenuFile menu count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
            return [];

        context.Blocks.AlignCurrent(4);
        byte[] pointerBytes = context.Blocks.Load(cursor, checked(count * sizeof(int)));
        var pointerCursor = new FastFileCursor(pointerBytes);
        var menus = new List<MenuDefReference>(count);

        for (int i = 0; i < count; i++)
        {
            int pointerRaw = pointerCursor.ReadInt32();
            XPointerReference menuPointer = context.PointerReader.FromRaw(pointerRaw, XPointerOffsetMode.AliasCell);
            MenuDefAsset? menu = ReadMenuDefPointer(cursor, menuPointer, context, out stopReason);
            menus.Add(new MenuDefReference(i, menuPointer.AsPointer<MenuDefAsset>(), menu));

            if (stopReason is not null)
                break;
        }

        return menus;
    }

    private static MenuDefAsset? ReadMenuDefPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context,
        out string? stopReason)
    {
        stopReason = null;

        if (!context.PointerReader.HasInlinePayload(pointer))
            return null;

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            context.Blocks.AlignCurrent(4);
            MenuDefAsset menu = ReadMenuDefRoot(cursor, context);
            stopReason =
                "stopped after proven MenuDef root: GPR proves child dispatch starts in LARGE, " +
                "but statement/expression helper semantics below MenuDef are not fully classified yet";
            return menu;
        }
        finally
        {
            context.Blocks.Pop();
        }
    }

    private static MenuDefAsset ReadMenuDefRoot(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, MenuDefSize);
        var rootCursor = new FastFileCursor(rootBytes);

        var menu = new MenuDefAsset
        {
            Offset = offset,
            Window = ReadWindow(rootCursor, context),
            FontPointer = ReadXStringPointer(rootCursor, context),
            Fullscreen = rootCursor.ReadInt32(),
            ItemCount = rootCursor.ReadInt32(),
            FontIndex = rootCursor.ReadInt32(),
            CursorItems = ReadInt32Array(rootCursor, 4),
            FadeCycle = rootCursor.ReadInt32(),
            FadeClamp = ReadSingle(rootCursor),
            FadeAmount = ReadSingle(rootCursor),
            FadeInAmount = ReadSingle(rootCursor),
            BlurRadius = ReadSingle(rootCursor),
            OnOpen = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            OnCloseRequest = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            OnClose = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            OnEsc = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            ExecKeys = ReadPointer<ItemKeyHandler>(rootCursor, context, XPointerResolutionMode.Direct),
            VisibleExpression = ReadPointer<Statement>(rootCursor, context, XPointerResolutionMode.Direct),
            AllowedBinding = ReadXStringPointer(rootCursor, context),
            SoundName = ReadXStringPointer(rootCursor, context),
            ImageTrack = rootCursor.ReadInt32(),
            FocusColor = ReadVec4(rootCursor),
            RectXExpression = ReadPointer<Statement>(rootCursor, context, XPointerResolutionMode.Direct),
            RectYExpression = ReadPointer<Statement>(rootCursor, context, XPointerResolutionMode.Direct),
            RectWExpression = ReadPointer<Statement>(rootCursor, context, XPointerResolutionMode.Direct),
            RectHExpression = ReadPointer<Statement>(rootCursor, context, XPointerResolutionMode.Direct),
            ItemsPointer = ReadPointer<XPointer<ItemDefAsset>[]>(rootCursor, context, XPointerResolutionMode.Direct),
            ScaleTransitions = ReadMenuTransitions(rootCursor, 4),
            AlphaTransitions = ReadMenuTransitions(rootCursor, 4),
            XTransitions = ReadMenuTransitions(rootCursor, 4),
            YTransitions = ReadMenuTransitions(rootCursor, 4),
            ExpressionData = ReadPointer<ExpressionSupportingData>(rootCursor, context, XPointerResolutionMode.Direct)
        };

        if (rootCursor.Offset != MenuDefSize)
            throw new InvalidDataException($"MenuDef consumed 0x{rootCursor.Offset:X} bytes instead of 0x{MenuDefSize:X}.");

        return menu;
    }

    private static string? ReadXString(
        FastFileCursor cursor,
        int pointerRaw,
        FastFileLoadContext context)
    {
        XPointerReference pointer = context.PointerReader.FromRaw(pointerRaw, XPointerOffsetMode.Direct);
        return context.PointerReader.HasInlinePayload(pointer)
            ? context.Blocks.LoadCString(cursor)
            : null;
    }

    private static WindowDef ReadWindow(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        return new WindowDef
        {
            NamePointer = ReadXStringPointer(cursor, context),
            Rect = ReadRectangle(cursor),
            RectClient = ReadRectangle(cursor),
            GroupPointer = ReadXStringPointer(cursor, context),
            Style = cursor.ReadInt32(),
            Border = cursor.ReadInt32(),
            OwnerDraw = cursor.ReadInt32(),
            OwnerDrawFlags = cursor.ReadInt32(),
            BorderSize = ReadSingle(cursor),
            StaticFlags = cursor.ReadInt32(),
            DynamicFlags = ReadInt32Array(cursor, 4),
            NextTime = cursor.ReadInt32(),
            ForeColor = ReadVec4(cursor),
            BackColor = ReadVec4(cursor),
            BorderColor = ReadVec4(cursor),
            OutlineColor = ReadVec4(cursor),
            DisableColor = ReadVec4(cursor),
            Background = ReadPointer<MaterialAsset>(cursor, context, XPointerResolutionMode.AliasCell)
        };
    }

    private static RectangleDef ReadRectangle(FastFileCursor cursor)
    {
        return new RectangleDef
        {
            X = ReadSingle(cursor),
            Y = ReadSingle(cursor),
            W = ReadSingle(cursor),
            H = ReadSingle(cursor),
            HorzAlign = (HorizontalAlign)cursor.ReadByte(),
            VertAlign = (VerticalAlign)cursor.ReadByte(),
            Pad12 = cursor.ReadUInt16()
        };
    }

    private static IReadOnlyList<MenuTransition> ReadMenuTransitions(FastFileCursor cursor, int count)
    {
        var transitions = new MenuTransition[count];
        for (int i = 0; i < transitions.Length; i++)
        {
            transitions[i] = new MenuTransition
            {
                TransitionType = cursor.ReadInt32(),
                TargetField = cursor.ReadInt32(),
                StartTime = cursor.ReadInt32(),
                StartValue = ReadSingle(cursor),
                EndValue = ReadSingle(cursor),
                Time = ReadSingle(cursor),
                EndTriggerType = cursor.ReadInt32()
            };
        }

        return transitions;
    }

    private static IReadOnlyList<int> ReadInt32Array(FastFileCursor cursor, int count)
    {
        var values = new int[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = cursor.ReadInt32();

        return values;
    }

    private static XPointer<string> ReadXStringPointer(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        return context.PointerReader.FromRaw<string>(cursor.ReadInt32(), XPointerResolutionMode.Direct);
    }

    private static XPointer<T> ReadPointer<T>(
        FastFileCursor cursor,
        FastFileLoadContext context,
        XPointerResolutionMode resolutionMode)
    {
        return context.PointerReader.FromRaw<T>(cursor.ReadInt32(), resolutionMode);
    }

    private static Vec4 ReadVec4(FastFileCursor cursor)
    {
        return new Vec4
        {
            A = ReadSingle(cursor),
            R = ReadSingle(cursor),
            G = ReadSingle(cursor),
            B = ReadSingle(cursor)
        };
    }

    private static float ReadSingle(FastFileCursor cursor)
    {
        return BitConverter.Int32BitsToSingle(cursor.ReadInt32());
    }
}
