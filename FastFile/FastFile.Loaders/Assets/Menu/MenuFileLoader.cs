using FastFile.Loaders.Assets.Material;
using FastFile.Models.Assets.Material;
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
    private const int ItemDefSize = ItemDefAsset.SerializedSize;
    private static readonly MaterialLoader MaterialLoader = new();

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
            AlignStream(cursor, context, 4);
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
        byte[] rootBytes = context.Blocks.Load(cursor, MenuFileSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        XPointer<string> namePointer = ReadXStringPointer(rootCursor, context);
        int menuCount = rootCursor.ReadInt32();
        XPointer<XPointer<MenuDefAsset>[]> menusPointer = ReadPointer<XPointer<MenuDefAsset>[]>(rootCursor, context, XPointerResolutionMode.Direct);

        if (rootCursor.Offset != MenuFileSize)
            throw new InvalidDataException($"MenuFile consumed 0x{rootCursor.Offset:X} bytes instead of 0x{MenuFileSize:X}.");

        context.Diagnostics.Trace(
            $"  MenuFile root source=0x{offset:X} name=0x{namePointer.Raw:X8} menuCount={menuCount} menus=0x{menusPointer.Raw:X8} blocks={context.Blocks.DescribePositions()}");

        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            string? name = ReadXString(cursor, namePointer, context);
            IReadOnlyList<MenuDefReference> menus = ReadMenuDefPointerArray(
                cursor,
                menusPointer.Untyped,
                menuCount,
                context,
                out stopReason);

            return new MenuFileAsset
            {
                Offset = offset,
                NamePointer = namePointer,
                Name = name,
                MenuCount = menuCount,
                MenusPointer = menusPointer,
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
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, checked(count * sizeof(int)), "MenuDef*[]");
            return [];
        }

        AlignStream(cursor, context, 4);
        int tableOffset = cursor.Offset;
        context.Diagnostics.Trace(
            $"    MenuFile.menus table source=0x{tableOffset:X} count={count} ptr={pointer} blocks={context.Blocks.DescribePositions()}");
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] pointerBytes = context.Blocks.Load(cursor, checked(count * sizeof(int)), out XBlockAddress pointerTableAddress);
        var pointerCursor = new FastFileCursor(pointerBytes, pointerTableAddress);
        var menus = new List<MenuDefReference>(count);

        for (int i = 0; i < count; i++)
        {
            XPointerReference menuPointer = context.PointerReader.ReadCell(pointerCursor, XPointerOffsetMode.Direct);
            context.Diagnostics.Trace(
                $"    MenuFile.menus[{i}] ptr={menuPointer} begin source=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()}");
            MenuDefAsset? menu = ReadMenuDefPointer(cursor, menuPointer, context);
            menus.Add(new MenuDefReference(i, menuPointer.AsPointer<MenuDefAsset>(), menu));
        }

        return menus;
    }

    private static MenuDefAsset? ReadMenuDefPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, MenuDefSize, "MenuDef");
            return null;
        }

        context.Blocks.Push(XFileBlockType.TEMP);
        try
        {
            XBlockAddress targetAddress = context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
            context.Diagnostics.Trace(
                $"    MenuDef pointer cell {pointer.CellAddress}=0x{XPointerCodec.Encode(targetAddress):X8} target={targetAddress}");
            MenuDefAsset menu = ReadMenuDefRoot(cursor, context);
            context.Blocks.Push(XFileBlockType.LARGE);
            try
            {
                ReadMenuDefChildren(cursor, menu, context);
            }
            finally
            {
                context.Blocks.Pop();
            }

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
        byte[] rootBytes = context.Blocks.Load(cursor, MenuDefSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

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

        context.Diagnostics.Trace(
            $"      MenuDef root source=0x{offset:X} itemCount={menu.ItemCount} font=0x{menu.FontPointer.Raw:X8} " +
            $"items=0x{menu.ItemsPointer.Raw:X8} expressionData=0x{menu.ExpressionData.Raw:X8} " +
            $"onOpen=0x{menu.OnOpen.Raw:X8} onCloseRequest=0x{menu.OnCloseRequest.Raw:X8} onClose=0x{menu.OnClose.Raw:X8} onEsc=0x{menu.OnEsc.Raw:X8} " +
            $"execKeys=0x{menu.ExecKeys.Raw:X8} visible=0x{menu.VisibleExpression.Raw:X8} " +
            $"rectExprs=[0x{menu.RectXExpression.Raw:X8},0x{menu.RectYExpression.Raw:X8},0x{menu.RectWExpression.Raw:X8},0x{menu.RectHExpression.Raw:X8}] " +
            $"window.name=0x{menu.Window.NamePointer.Raw:X8} window.group=0x{menu.Window.GroupPointer.Raw:X8} window.background=0x{menu.Window.Background.Raw:X8} " +
            $"blocks={context.Blocks.DescribePositions()}");

        return menu;
    }

    private static void ReadMenuDefChildren(
        FastFileCursor cursor,
        MenuDefAsset menu,
        FastFileLoadContext context)
    {
        menu.ExpressionDataValue = ReadExpressionSupportingDataPointer(cursor, menu.ExpressionData.Untyped, context);
        ReadWindowChildren(cursor, menu.Window, context);
        menu.Font = ReadXString(cursor, menu.FontPointer, context);

        menu.OnOpenSet = ReadMenuEventHandlerSetPointer(cursor, menu.OnOpen.Untyped, context);
        menu.OnCloseSet = ReadMenuEventHandlerSetPointer(cursor, menu.OnClose.Untyped, context);
        menu.OnCloseRequestSet = ReadMenuEventHandlerSetPointer(cursor, menu.OnCloseRequest.Untyped, context);
        menu.OnEscSet = ReadMenuEventHandlerSetPointer(cursor, menu.OnEsc.Untyped, context);

        menu.ExecKeyHandler = ReadItemKeyHandlerPointer(cursor, menu.ExecKeys.Untyped, context);
        menu.VisibleStatement = ReadStatementPointer(cursor, menu.VisibleExpression.Untyped, context);
        menu.AllowedBindingString = ReadXString(cursor, menu.AllowedBinding, context);
        menu.SoundNameString = ReadXString(cursor, menu.SoundName, context);
        menu.RectXStatement = ReadStatementPointer(cursor, menu.RectXExpression.Untyped, context);
        menu.RectYStatement = ReadStatementPointer(cursor, menu.RectYExpression.Untyped, context);
        menu.RectWStatement = ReadStatementPointer(cursor, menu.RectWExpression.Untyped, context);
        menu.RectHStatement = ReadStatementPointer(cursor, menu.RectHExpression.Untyped, context);

        if (menu is { ItemCount: >= 0 })
        {
            IReadOnlyList<ItemDefReference> items = ReadItemDefPointerArray(
                cursor,
                menu.ItemsPointer.Untyped,
                menu.ItemCount,
                context);

            menu.Items = items;
        }
    }

    private static string? ReadXString(
        FastFileCursor cursor,
        XPointer<string> pointer,
        FastFileLoadContext context)
    {
        return context.PointerReader.LoadXString(cursor, pointer);
    }

    private static void ReadWindowChildren(
        FastFileCursor cursor,
        WindowDef window,
        FastFileLoadContext context)
    {
        window.Name = ReadXString(cursor, window.NamePointer, context);
        window.Group = ReadXString(cursor, window.GroupPointer, context);
        ReadMaterialPointer(cursor, window.Background.Untyped, context);
    }

    private static void ReadMaterialPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        MaterialLoader.LoadFromPointer(cursor, pointer, context);
    }

    private static IReadOnlyList<ItemDefReference> ReadItemDefPointerArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative ItemDef count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, checked(count * sizeof(int)), "ItemDef*[]");
            return [];
        }

        AlignStream(cursor, context, 4);
        int tableOffset = cursor.Offset;
        context.Diagnostics.Trace(
            $"        MenuDef.items table source=0x{tableOffset:X} count={count} ptr={pointer} blocks={context.Blocks.DescribePositions()}");
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] pointerBytes = context.Blocks.Load(cursor, checked(count * sizeof(int)), out XBlockAddress tableAddress);
        var pointerCursor = new FastFileCursor(pointerBytes, tableAddress);
        var items = new ItemDefReference[count];
        ItemDefAsset? previousItem = null;
        int previousEndOffset = cursor.Offset;
        var recentItems = new Queue<string>();

        for (int i = 0; i < items.Length; i++)
        {
            XPointerReference itemPointer = context.PointerReader.ReadCell(pointerCursor, XPointerOffsetMode.Direct);
            context.Diagnostics.Trace(
                $"        MenuDef.items[{i}] ptr={itemPointer} begin source=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()}");
            ItemDefAsset? item;
            try
            {
                item = ReadItemDefPointer(cursor, itemPointer, context);
            }
            catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException or OverflowException)
            {
                throw new InvalidDataException(
                    $"ItemDef[{i}] pointer 0x{itemPointer.Raw:X8} failed at cursor 0x{cursor.Offset:X}. " +
                    $"Previous item was {(previousItem is null ? "<none>" : $"source 0x{previousItem.Offset:X}..0x{previousEndOffset:X} type={previousItem.Type} dataType=0x{previousItem.DataType:X8} typeData=0x{previousItem.TypeData.RawPointer.Raw:X8}")}. " +
                    $"Recent items: {string.Join("; ", recentItems)}.",
                    ex);
            }

            items[i] = new ItemDefReference(i, itemPointer.AsPointer<ItemDefAsset>(), item);
            if (item is not null)
            {
                previousItem = item;
                previousEndOffset = cursor.Offset;
                recentItems.Enqueue($"[{i}] 0x{item.Offset:X}..0x{previousEndOffset:X} type={item.Type} data=0x{item.DataType:X8} typeData=0x{item.TypeData.RawPointer.Raw:X8}");
                while (recentItems.Count > 8)
                    recentItems.Dequeue();
            }
        }

        return items;
    }

    private static ItemDefAsset? ReadItemDefPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, ItemDefSize, "ItemDef");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        ItemDefAsset item = ReadItemDefRoot(cursor, context);
        context.Blocks.Push(XFileBlockType.LARGE);
        try
        {
            ReadItemDefChildren(cursor, item, context);
        }
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException or OverflowException or InvalidOperationException)
        {
            throw new InvalidDataException(
                $"ItemDef root at source 0x{item.Offset:X} parsed before child failure at cursor 0x{cursor.Offset:X}: " +
                $"type={item.Type} dataType=0x{item.DataType:X8} text=0x{item.Text.Raw:X8} textSaveGameInfo=0x{item.TextSaveGameInfo:X8} " +
                $"runtimeParent=0x{item.RuntimeParentPointer:X8} mouseEnterText=0x{item.MouseEnterText.Raw:X8} " +
                $"typeData=0x{item.TypeData.RawPointer.Raw:X8} floatCount=0x{item.FloatExpressionCount:X8} " +
                $"floatExpressions=0x{item.FloatExpressions.Raw:X8} visible=0x{item.VisibleExpression.Raw:X8} " +
                $"disabled=0x{item.DisabledExpression.Raw:X8} textExpr=0x{item.TextExpression.Raw:X8} " +
                $"materialExpr=0x{item.MaterialExpression.Raw:X8} background=0x{item.Window.Background.Raw:X8}.",
                ex);
        }
        finally
        {
            context.Blocks.Pop();
        }

        return item;
    }

    private static ItemDefAsset ReadItemDefRoot(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int offset = cursor.Offset;
        byte[] rootBytes = context.Blocks.Load(cursor, ItemDefSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var item = new ItemDefAsset
        {
            Offset = offset,
            Window = ReadWindow(rootCursor, context),
            TextRect = ReadRectangles(rootCursor, 4),
            Type = (ItemDefType)rootCursor.ReadInt32(),
            DataType = rootCursor.ReadInt32(),
            Align = rootCursor.ReadInt32(),
            FontEnum = rootCursor.ReadInt32(),
            TextAlignMode = rootCursor.ReadInt32(),
            TextAlignX = ReadSingle(rootCursor),
            TextAlignY = ReadSingle(rootCursor),
            TextScale = ReadSingle(rootCursor),
            TextStyle = rootCursor.ReadInt32(),
            GameMsgWindowIndex = rootCursor.ReadInt32(),
            GameMsgWindowMode = rootCursor.ReadInt32(),
            Text = ReadXStringPointer(rootCursor, context),
            TextSaveGameInfo = rootCursor.ReadInt32(),
            RuntimeParentPointer = rootCursor.ReadInt32(),
            MouseEnterText = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            MouseExitText = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            MouseEnter = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            MouseExit = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            Action = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            Accept = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            OnFocus = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            LeaveFocus = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            Dvar = ReadXStringPointer(rootCursor, context),
            DvarTest = ReadXStringPointer(rootCursor, context),
            OnKey = ReadPointer<ItemKeyHandler>(rootCursor, context, XPointerResolutionMode.Direct),
            EnableDvar = ReadXStringPointer(rootCursor, context),
            DvarFlags = rootCursor.ReadInt32(),
            FocusSound = ReadPointer<SoundAliasListAsset>(rootCursor, context, XPointerResolutionMode.AliasCell),
            Special = ReadSingle(rootCursor),
            CursorPos = ReadInt32Array(rootCursor, 4),
            TypeData = new ItemDefData
            {
                RawPointer = ReadRawCell(rootCursor, XPointerOffsetMode.Direct)
            },
            ImageTrack = rootCursor.ReadInt32(),
            FloatExpressionCount = rootCursor.ReadInt32(),
            FloatExpressions = ReadPointer<ItemFloatExpression[]>(rootCursor, context, XPointerResolutionMode.Direct),
            VisibleExpression = ReadPointer<Statement>(rootCursor, context, XPointerResolutionMode.Direct),
            DisabledExpression = ReadPointer<Statement>(rootCursor, context, XPointerResolutionMode.Direct),
            TextExpression = ReadPointer<Statement>(rootCursor, context, XPointerResolutionMode.Direct),
            MaterialExpression = ReadPointer<Statement>(rootCursor, context, XPointerResolutionMode.Direct),
            GlowColor = ReadVec4(rootCursor),
            DecayActive = rootCursor.ReadByte(),
            DecayActivePad0 = rootCursor.ReadByte(),
            DecayActivePad1 = rootCursor.ReadByte(),
            DecayActivePad2 = rootCursor.ReadByte(),
            FxBirthTime = rootCursor.ReadInt32(),
            FxLetterTime = rootCursor.ReadInt32(),
            FxDecayStartTime = rootCursor.ReadInt32(),
            FxDecayDuration = rootCursor.ReadInt32(),
            LastSoundPlayedTime = rootCursor.ReadInt32()
        };

        if (rootCursor.Offset != ItemDefSize)
            throw new InvalidDataException($"ItemDef consumed 0x{rootCursor.Offset:X} bytes instead of 0x{ItemDefSize:X}.");

        if (item.FloatExpressionCount is < 0 or > 0x1000)
        {
            throw new InvalidDataException(
                $"ItemDef at source 0x{item.Offset:X} has invalid floatExpressionCount 0x{item.FloatExpressionCount:X8}; " +
                $"type={item.Type} dataType=0x{item.DataType:X8} typeData=0x{item.TypeData.RawPointer.Raw:X8} " +
                $"floatExpressions=0x{item.FloatExpressions.Raw:X8} visible=0x{item.VisibleExpression.Raw:X8}.");
        }

        context.Diagnostics.Trace(
            $"          ItemDef root source=0x{offset:X} type={item.Type} dataType=0x{item.DataType:X8} " +
            $"text=0x{item.Text.Raw:X8} runtimeParent=0x{item.RuntimeParentPointer:X8} typeData=0x{item.TypeData.RawPointer.Raw:X8} " +
            $"floatCount={item.FloatExpressionCount} floatExpressions=0x{item.FloatExpressions.Raw:X8} " +
            $"visible=0x{item.VisibleExpression.Raw:X8} disabled=0x{item.DisabledExpression.Raw:X8} " +
            $"textExpr=0x{item.TextExpression.Raw:X8} materialExpr=0x{item.MaterialExpression.Raw:X8} " +
            $"window.name=0x{item.Window.NamePointer.Raw:X8} window.group=0x{item.Window.GroupPointer.Raw:X8} window.background=0x{item.Window.Background.Raw:X8} " +
            $"blocks={context.Blocks.DescribePositions()}");

        return item;
    }

    private static void ReadItemDefChildren(
        FastFileCursor cursor,
        ItemDefAsset item,
        FastFileLoadContext context)
    {
        ReadWindowChildren(cursor, item.Window, context);
        item.TextString = ReadXString(cursor, item.Text, context);
        item.MouseEnterTextSet = ReadMenuEventHandlerSetPointer(cursor, item.MouseEnterText.Untyped, context);
        item.MouseExitTextSet = ReadMenuEventHandlerSetPointer(cursor, item.MouseExitText.Untyped, context);
        item.MouseEnterSet = ReadMenuEventHandlerSetPointer(cursor, item.MouseEnter.Untyped, context);
        item.MouseExitSet = ReadMenuEventHandlerSetPointer(cursor, item.MouseExit.Untyped, context);
        item.ActionSet = ReadMenuEventHandlerSetPointer(cursor, item.Action.Untyped, context);
        item.AcceptSet = ReadMenuEventHandlerSetPointer(cursor, item.Accept.Untyped, context);
        item.OnFocusSet = ReadMenuEventHandlerSetPointer(cursor, item.OnFocus.Untyped, context);
        item.LeaveFocusSet = ReadMenuEventHandlerSetPointer(cursor, item.LeaveFocus.Untyped, context);
        item.DvarString = ReadXString(cursor, item.Dvar, context);
        item.DvarTestString = ReadXString(cursor, item.DvarTest, context);
        item.OnKeyHandler = ReadItemKeyHandlerPointer(cursor, item.OnKey.Untyped, context);
        item.EnableDvarString = ReadXString(cursor, item.EnableDvar, context);
        WarnIfUnsupportedInline(item.FocusSound.Untyped, nameof(ItemDefAsset.FocusSound), context);
        ReadItemTypeData(cursor, item, context);

        IReadOnlyList<ItemFloatExpression> floatExpressions = ReadItemFloatExpressions(
            cursor,
            item.FloatExpressions.Untyped,
            item.FloatExpressionCount,
            context);
        item.LoadedFloatExpressions = floatExpressions;

        item.VisibleStatement = ReadStatementPointer(cursor, item.VisibleExpression.Untyped, context);
        item.DisabledStatement = ReadStatementPointer(cursor, item.DisabledExpression.Untyped, context);
        item.TextStatement = ReadStatementPointer(cursor, item.TextExpression.Untyped, context);
        item.MaterialStatement = ReadStatementPointer(cursor, item.MaterialExpression.Untyped, context);
    }

    private static IReadOnlyList<RectangleDef> ReadRectangles(FastFileCursor cursor, int count)
    {
        var rectangles = new RectangleDef[count];
        for (int i = 0; i < rectangles.Length; i++)
            rectangles[i] = ReadRectangle(cursor);

        return rectangles;
    }

    private static WindowDef ReadWindow(
        FastFileCursor cursor,
        FastFileLoadContext context)
    {
        int start = cursor.Offset;
        var window = new WindowDef
        {
            NamePointer = ReadXStringPointer(cursor, context),
            Rect = ReadRectangle(cursor),
            RectClient = ReadRectangle(cursor),
            GroupPointer = ReadXStringPointer(cursor, context),
            Style = (WindowStyle)cursor.ReadInt32(),
            Border = (WindowBorder)cursor.ReadInt32(),
            OwnerDraw = (WindowOwnerDraw)cursor.ReadInt32(),
            OwnerDrawFlags = cursor.ReadInt32(),
            BorderSize = ReadSingle(cursor),
            StaticFlags = (WindowStaticFlags)cursor.ReadInt32(),
            DynamicFlags = ReadWindowDynamicFlags(cursor),
            NextTime = cursor.ReadInt32(),
            ForeColor = ReadVec4(cursor),
            BackColor = ReadVec4(cursor),
            BorderColor = ReadVec4(cursor),
            OutlineColor = ReadVec4(cursor),
            DisableColor = ReadVec4(cursor),
            Background = ReadPointer<MaterialAsset>(cursor, context, WindowDefContract.Background.ResolutionMode)
        };

        int consumed = cursor.Offset - start;
        if (consumed != WindowDefContract.SerializedSize)
            throw new InvalidDataException($"WindowDef consumed 0x{consumed:X} bytes instead of 0x{WindowDefContract.SerializedSize:X}.");

        return window;
    }

    private static RectangleDef ReadRectangle(FastFileCursor cursor)
    {
        int start = cursor.Offset;
        var rectangle = new RectangleDef
        {
            X = ReadSingle(cursor),
            Y = ReadSingle(cursor),
            W = ReadSingle(cursor),
            H = ReadSingle(cursor),
            HorzAlign = (HorizontalAlign)cursor.ReadByte(),
            VertAlign = (VerticalAlign)cursor.ReadByte(),
            Pad12 = cursor.ReadUInt16()
        };

        int consumed = cursor.Offset - start;
        if (consumed != RectangleDefContract.SerializedSize)
            throw new InvalidDataException($"RectangleDef consumed 0x{consumed:X} bytes instead of 0x{RectangleDefContract.SerializedSize:X}.");

        return rectangle;
    }

    private static IReadOnlyList<WindowDynamicFlags> ReadWindowDynamicFlags(FastFileCursor cursor)
    {
        WindowDynamicFlags[] values = new WindowDynamicFlags[4];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = (WindowDynamicFlags)cursor.ReadInt32();
        }

        return values;
    }

    private static IReadOnlyList<MenuTransition> ReadMenuTransitions(FastFileCursor cursor, int count)
    {
        var transitions = new MenuTransition[count];
        for (int i = 0; i < transitions.Length; i++)
        {
            transitions[i] = new MenuTransition
            {
                TransitionType = (MenuTransitionType)cursor.ReadInt32(),
                TargetField = cursor.ReadInt32(),
                StartTime = cursor.ReadInt32(),
                StartValue = ReadSingle(cursor),
                EndValue = ReadSingle(cursor),
                Time = ReadSingle(cursor),
                EndTriggerType = (MenuTransitionEndTrigger)cursor.ReadInt32()
            };
        }

        return transitions;
    }

    private static MenuEventHandlerSet? ReadMenuEventHandlerSetPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, MenuEventHandlerSet.SerializedSize, "MenuEventHandlerSet");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, MenuEventHandlerSet.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var set = new MenuEventHandlerSet
        {
            EventHandlerCount = rootCursor.ReadInt32(),
            EventHandlers = ReadPointer<XPointer<MenuEventHandler>[]>(rootCursor, context, XPointerResolutionMode.Direct)
        };

        if (rootCursor.Offset != MenuEventHandlerSet.SerializedSize)
            throw new InvalidDataException($"MenuEventHandlerSet consumed 0x{rootCursor.Offset:X} bytes instead of 0x{MenuEventHandlerSet.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"            MenuEventHandlerSet root handlers={set.EventHandlerCount} events=0x{set.EventHandlers.Raw:X8} blocks={context.Blocks.DescribePositions()}");

        set.Handlers = ReadMenuEventHandlerPointerArray(cursor, set.EventHandlers.Untyped, set.EventHandlerCount, context);
        return set;
    }

    private static IReadOnlyList<MenuEventHandlerReference> ReadMenuEventHandlerPointerArray(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative MenuEventHandler count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, checked(count * sizeof(int)), "MenuEventHandler*[]");
            return [];
        }

        AlignStream(cursor, context, 4);
        int tableOffset = cursor.Offset;
        context.Diagnostics.Trace(
            $"              MenuEventHandlerSet.events table source=0x{tableOffset:X} count={count} ptr={pointer} blocks={context.Blocks.DescribePositions()}");
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] pointerBytes = context.Blocks.Load(cursor, checked(count * sizeof(int)), out XBlockAddress tableAddress);
        var pointerCursor = new FastFileCursor(pointerBytes, tableAddress);
        var handlers = new MenuEventHandlerReference[count];

        for (int i = 0; i < count; i++)
        {
            XPointerReference handlerPointer = context.PointerReader.ReadCell(pointerCursor, XPointerOffsetMode.Direct);
            context.Diagnostics.Trace(
                $"              MenuEventHandlerSet.events[{i}] ptr={handlerPointer} begin source=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()}");
            MenuEventHandler? handler = ReadMenuEventHandlerPointer(cursor, handlerPointer, context);
            handlers[i] = new MenuEventHandlerReference(i, handlerPointer.AsPointer<MenuEventHandler>(), handler);
        }

        return handlers;
    }

    private static MenuEventHandler? ReadMenuEventHandlerPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, MenuEventHandler.SerializedSize, "MenuEventHandler");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, MenuEventHandler.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var handler = new MenuEventHandler
        {
            EventData = new EventData
            {
                Data = context.PointerReader.ReadCell(rootCursor, XPointerOffsetMode.Direct)
            },
            EventType = (MenuEventHandlerType)rootCursor.ReadByte(),
            Pad05 = rootCursor.ReadByte(),
            Pad06 = rootCursor.ReadByte(),
            Pad07 = rootCursor.ReadByte()
        };

        if (rootCursor.Offset != MenuEventHandler.SerializedSize)
            throw new InvalidDataException($"MenuEventHandler consumed 0x{rootCursor.Offset:X} bytes instead of 0x{MenuEventHandler.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"                MenuEventHandler root type={handler.EventType} data=0x{handler.EventData.Data.Raw:X8} blocks={context.Blocks.DescribePositions()}");

        ReadEventData(cursor, handler, rootAddress, context);
        return handler;
    }

    private static void ReadEventData(
        FastFileCursor cursor,
        MenuEventHandler handler,
        XBlockAddress rootAddress,
        FastFileLoadContext context)
    {
        XBlockAddress dataCellAddress = rootAddress.Add(0x00);

        switch (handler.EventType)
        {
            case MenuEventHandlerType.UnconditionalScript:
                handler.UnconditionalScript = context.PointerReader.LoadXString(cursor, dataCellAddress, handler.EventData.UnconditionalScript);
                break;

            case MenuEventHandlerType.ConditionalScript:
                if (context.PointerReader.HasInlinePayload(handler.EventData.ConditionalScript.Untyped))
                    context.PointerReader.PatchInlinePointerCell(dataCellAddress, handler.EventData.ConditionalScript.Raw, alignment: 4);

                handler.ConditionalScript = ReadConditionalScriptPointer(cursor, handler.EventData.ConditionalScript.Untyped, context);
                break;

            case MenuEventHandlerType.ElseScript:
                if (context.PointerReader.HasInlinePayload(handler.EventData.ElseScript.Untyped))
                    context.PointerReader.PatchInlinePointerCell(dataCellAddress, handler.EventData.ElseScript.Raw, alignment: 4);

                handler.ElseScriptSet = ReadMenuEventHandlerSetPointer(cursor, handler.EventData.ElseScript.Untyped, context);
                break;

            case MenuEventHandlerType.SetLocalVarBool:
            case MenuEventHandlerType.SetLocalVarInt:
            case MenuEventHandlerType.SetLocalVarFloat:
            case MenuEventHandlerType.SetLocalVarString:
                if (context.PointerReader.HasInlinePayload(handler.EventData.SetLocalVarData.Untyped))
                    context.PointerReader.PatchInlinePointerCell(dataCellAddress, handler.EventData.SetLocalVarData.Raw, alignment: 4);

                handler.SetLocalVarData = ReadSetLocalVarDataPointer(cursor, handler.EventData.SetLocalVarData.Untyped, context);
                break;
        }
    }

    private static ConditionalScript? ReadConditionalScriptPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, ConditionalScript.SerializedSize, "ConditionalScript");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, ConditionalScript.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var script = new ConditionalScript
        {
            EventHandlerSet = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            EventExpression = ReadPointer<Statement>(rootCursor, context, XPointerResolutionMode.Direct)
        };

        if (rootCursor.Offset != ConditionalScript.SerializedSize)
            throw new InvalidDataException($"ConditionalScript consumed 0x{rootCursor.Offset:X} bytes instead of 0x{ConditionalScript.SerializedSize:X}.");

        script.EventStatement = ReadStatementPointer(cursor, script.EventExpression.Untyped, context);

        script.EventHandlers = ReadMenuEventHandlerSetPointer(cursor, script.EventHandlerSet.Untyped, context);
        return script;
    }

    private static SetLocalVarData? ReadSetLocalVarDataPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, SetLocalVarData.SerializedSize, "SetLocalVarData");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, SetLocalVarData.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var data = new SetLocalVarData
        {
            LocalVarName = ReadXStringPointer(rootCursor, context),
            Expression = ReadPointer<Statement>(rootCursor, context, XPointerResolutionMode.Direct)
        };

        if (rootCursor.Offset != SetLocalVarData.SerializedSize)
            throw new InvalidDataException($"SetLocalVarData consumed 0x{rootCursor.Offset:X} bytes instead of 0x{SetLocalVarData.SerializedSize:X}.");

        data.LocalVarNameString = context.PointerReader.LoadXString(cursor, data.LocalVarName);
        data.ExpressionStatement = ReadStatementPointer(cursor, data.Expression.Untyped, context);
        return data;
    }

    private static ItemKeyHandler? ReadItemKeyHandlerPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, ItemKeyHandler.SerializedSize, "ItemKeyHandler");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, ItemKeyHandler.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var handler = new ItemKeyHandler
        {
            Key = rootCursor.ReadInt32(),
            Action = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            Next = ReadPointer<ItemKeyHandler>(rootCursor, context, XPointerResolutionMode.Direct)
        };

        if (rootCursor.Offset != ItemKeyHandler.SerializedSize)
            throw new InvalidDataException($"ItemKeyHandler consumed 0x{rootCursor.Offset:X} bytes instead of 0x{ItemKeyHandler.SerializedSize:X}.");

        handler.ActionSet = ReadMenuEventHandlerSetPointer(cursor, handler.Action.Untyped, context);
        handler.NextHandler = ReadItemKeyHandlerPointer(cursor, handler.Next.Untyped, context);
        return handler;
    }

    private static Statement? ReadStatementPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, Statement.SerializedSize, "Statement");
            VerifyOffsetStatementPointer(pointer, context);
            return null;
        }

        AlignStream(cursor, context, 4);
        int offset = cursor.Offset;
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, Statement.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var statement = new Statement
        {
            NumEntries = rootCursor.ReadInt32(),
            Entries = ReadPointer<ExpressionEntry[]>(rootCursor, context, XPointerResolutionMode.Direct),
            SupportingData = ReadPointer<ExpressionSupportingData>(rootCursor, context, XPointerResolutionMode.Direct),
            LastExecuteTime = rootCursor.ReadInt32(),
            LastResult = ReadOperand(rootCursor, context)
        };

        if (rootCursor.Offset != Statement.SerializedSize)
            throw new InvalidDataException($"Statement consumed 0x{rootCursor.Offset:X} bytes instead of 0x{Statement.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"            Statement root source=0x{offset:X} entries={statement.NumEntries} entriesPtr=0x{statement.Entries.Raw:X8} " +
            $"supportingData=0x{statement.SupportingData.Raw:X8} lastExecute=0x{statement.LastExecuteTime:X8} " +
            $"lastResultType={statement.LastResult.DataType} lastResultRaw=0x{statement.LastResult.Internals.Raw:X8} blocks={context.Blocks.DescribePositions()}");

        if (statement.NumEntries is < 0 or > 0x10000)
        {
            throw new InvalidDataException(
                $"Statement at source 0x{offset:X} from pointer 0x{pointer.Raw:X8} has invalid numEntries 0x{statement.NumEntries:X8}; " +
                $"entries=0x{statement.Entries.Raw:X8}, supportingData=0x{statement.SupportingData.Raw:X8}.");
        }

        statement.LoadedEntries = ReadExpressionEntries(cursor, statement.Entries.Untyped, statement.NumEntries, context);

        statement.SupportingDataValue = ReadExpressionSupportingDataPointer(cursor, statement.SupportingData.Untyped, context);
        return statement;
    }

    private static void VerifyOffsetStatementPointer(
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (pointer.PackedAddress is not { } address)
            return;

        if (address.BlockType is not XFileBlockType.LARGE)
        {
            context.Diagnostics.Warn(
                $"Statement* offset pointer 0x{pointer.Raw:X8} targets {address}; expected LARGE block for menu expression data.");
            return;
        }

        byte[] blockBytes = context.Blocks.GetBytes(address.BlockType);
        if ((uint)address.Offset > (uint)(blockBytes.Length - Statement.SerializedSize))
        {
            context.Diagnostics.Warn(
                $"Statement* offset pointer 0x{pointer.Raw:X8} targets {address}, but {address.BlockType} currently has 0x{blockBytes.Length:X} byte(s).");
            return;
        }

        var cursor = new FastFileCursor(blockBytes.AsMemory(address.Offset, Statement.SerializedSize));
        int numEntries = cursor.ReadInt32();
        int entriesRaw = cursor.ReadInt32();
        int supportingDataRaw = cursor.ReadInt32();

        if (numEntries is < 0 or > 0x10000)
        {
            context.Diagnostics.Warn(
                $"Statement* offset pointer 0x{pointer.Raw:X8} targets {address}, but Statement.NumEntries is implausible: 0x{numEntries:X8}; " +
                $"entries=0x{entriesRaw:X8}, supportingData=0x{supportingDataRaw:X8}.");
        }
    }

    private static IReadOnlyList<ExpressionEntry> ReadExpressionEntries(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative ExpressionEntry count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, checked(count * ExpressionEntry.SerializedSize), "ExpressionEntry[]");
            return [];
        }

        AlignStream(cursor, context, 4);
        context.Diagnostics.Trace(
            $"              ExpressionEntry table source=0x{cursor.Offset:X} count={count} ptr={pointer} blocks={context.Blocks.DescribePositions()}");
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] entryBytes = context.Blocks.Load(cursor, checked(count * ExpressionEntry.SerializedSize), out XBlockAddress tableAddress);
        var entryCursor = new FastFileCursor(entryBytes, tableAddress);
        var entries = new ExpressionEntry[count];

        for (int i = 0; i < entries.Length; i++)
        {
            int rowStart = entryCursor.Offset;
            var kind = (ExpressionEntryKind)entryCursor.ReadInt32();
            Operand operand = ReadOperand(entryCursor, context);

            if (entryCursor.Offset - rowStart != ExpressionEntry.SerializedSize)
                throw new InvalidDataException($"ExpressionEntry consumed 0x{entryCursor.Offset - rowStart:X} bytes instead of 0x{ExpressionEntry.SerializedSize:X}.");

            var entry = new ExpressionEntry
            {
                Kind = kind,
                Operand = operand
            };
            entries[i] = entry;

            if (kind != ExpressionEntryKind.Operand)
                continue;

            ReadOperandChildren(cursor, entry, tableAddress.Add(rowStart + 0x08), context);
        }

        return entries;
    }

    private static Operand ReadOperand(FastFileCursor cursor, FastFileLoadContext context)
    {
        return new Operand
        {
            DataType = (ExpDataType)cursor.ReadInt32(),
            Internals = new OperandInternalData(cursor.ReadInt32())
        };
    }

    private static XPointerReference ReadRawCell(
        FastFileCursor cursor,
        XPointerOffsetMode offsetMode)
    {
        int cellOffset = cursor.Offset;
        return XPointerReference.FromRaw(
            cursor.ReadInt32(),
            offsetMode,
            cursor.AddressAt(cellOffset));
    }

    private static void ReadOperandChildren(
        FastFileCursor cursor,
        ExpressionEntry entry,
        XBlockAddress pointerCellAddress,
        FastFileLoadContext context)
    {
        Operand operand = entry.Operand;
        switch (operand.DataType)
        {
            case ExpDataType.VAL_STRING:
                entry.StringValue = context.PointerReader.LoadXString(
                    cursor,
                    context.PointerReader.FromRaw<string>(
                        operand.Internals.Raw,
                        XPointerResolutionMode.Direct,
                        pointerCellAddress));
                break;

            case ExpDataType.VAL_FUNCTION:
                entry.FunctionStatement = ReadStatementPointer(
                    cursor,
                    context.PointerReader.FromRaw<Statement>(
                        operand.Internals.Raw,
                        XPointerResolutionMode.Direct,
                        pointerCellAddress).Untyped,
                    context);
                break;
        }
    }

    private static ExpressionString? ReadExpressionStringPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, ExpressionString.SerializedSize, "ExpressionString");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, ExpressionString.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var expressionString = new ExpressionString
        {
            String = ReadXStringPointer(rootCursor, context)
        };

        if (rootCursor.Offset != ExpressionString.SerializedSize)
            throw new InvalidDataException($"ExpressionString consumed 0x{rootCursor.Offset:X} bytes instead of 0x{ExpressionString.SerializedSize:X}.");

        context.PointerReader.LoadXString(cursor, expressionString.String);
        return expressionString;
    }

    private static ExpressionSupportingData? ReadExpressionSupportingDataPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, ExpressionSupportingData.SerializedSize, "ExpressionSupportingData");
            return null;
        }

        AlignStream(cursor, context, 4);
        int offset = cursor.Offset;
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, ExpressionSupportingData.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var data = new ExpressionSupportingData
        {
            UiFunctions = ReadUiFunctionList(rootCursor, context),
            StaticDvarList = ReadStaticDvarList(rootCursor, context),
            UiStrings = ReadStringList(rootCursor, context)
        };

        if (rootCursor.Offset != ExpressionSupportingData.SerializedSize)
            throw new InvalidDataException($"ExpressionSupportingData consumed 0x{rootCursor.Offset:X} bytes instead of 0x{ExpressionSupportingData.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"            ExpressionSupportingData root source=0x{offset:X} " +
            $"uiFunctions.count={data.UiFunctions.TotalFunctions} ptr=0x{data.UiFunctions.Functions.Raw:X8} " +
            $"staticDvars.count={data.StaticDvarList.NumStaticDvars} ptr=0x{data.StaticDvarList.StaticDvars.Raw:X8} " +
            $"strings.count={data.UiStrings.TotalStrings} ptr=0x{data.UiStrings.Strings.Raw:X8} blocks={context.Blocks.DescribePositions()}");

        data.UiFunctions.LoadedFunctions = ReadUiFunctionListChildren(cursor, data.UiFunctions, context);
        data.StaticDvarList.LoadedStaticDvars = ReadStaticDvarListChildren(cursor, data.StaticDvarList, context);
        data.UiStrings.LoadedStrings = ReadStringListChildren(cursor, data.UiStrings, context);
        return data;
    }

    private static UIFunctionList ReadUiFunctionList(FastFileCursor cursor, FastFileLoadContext context)
    {
        return new UIFunctionList
        {
            TotalFunctions = cursor.ReadInt32(),
            Functions = ReadPointer<XPointer<Statement>[]>(cursor, context, XPointerResolutionMode.Direct)
        };
    }

    private static StaticDvarList ReadStaticDvarList(FastFileCursor cursor, FastFileLoadContext context)
    {
        return new StaticDvarList
        {
            NumStaticDvars = cursor.ReadInt32(),
            StaticDvars = ReadPointer<XPointer<StaticDvar>[]>(cursor, context, XPointerResolutionMode.Direct)
        };
    }

    private static StringList ReadStringList(FastFileCursor cursor, FastFileLoadContext context)
    {
        return new StringList
        {
            TotalStrings = cursor.ReadInt32(),
            Strings = ReadPointer<XPointer<string>[]>(cursor, context, XPointerResolutionMode.Direct)
        };
    }

    private static IReadOnlyList<StatementReference> ReadUiFunctionListChildren(
        FastFileCursor cursor,
        UIFunctionList list,
        FastFileLoadContext context)
    {
        if (context.PointerReader.HasInlinePayload(list.Functions.Untyped))
            context.PointerReader.PatchInlinePointerCell(list.Functions, alignment: 4);

        return ReadPointerArray(cursor, list.Functions.Untyped, list.TotalFunctions, context, "UIFunctionList.functions", (index, pointer) =>
            new StatementReference(index, pointer.AsPointer<Statement>(), ReadStatementPointer(cursor, pointer, context)), inlineAlignment: 4);
    }

    private static IReadOnlyList<StaticDvarReference> ReadStaticDvarListChildren(
        FastFileCursor cursor,
        StaticDvarList list,
        FastFileLoadContext context)
    {
        if (context.PointerReader.HasInlinePayload(list.StaticDvars.Untyped))
            context.PointerReader.PatchInlinePointerCell(list.StaticDvars, alignment: 4);

        return ReadPointerArray(cursor, list.StaticDvars.Untyped, list.NumStaticDvars, context, "StaticDvarList.staticDvars", (index, pointer) =>
            new StaticDvarReference(index, pointer.AsPointer<StaticDvar>(), ReadStaticDvarPointer(cursor, pointer, context)), inlineAlignment: 4);
    }

    private static IReadOnlyList<XStringReference> ReadStringListChildren(
        FastFileCursor cursor,
        StringList list,
        FastFileLoadContext context)
    {
        if (context.PointerReader.HasInlinePayload(list.Strings.Untyped))
            context.PointerReader.PatchInlinePointerCell(list.Strings, alignment: 4);

        return ReadPointerArray(cursor, list.Strings.Untyped, list.TotalStrings, context, "StringList.strings", (index, pointer) =>
            new XStringReference(index, pointer.AsPointer<string>(), ReadXString(cursor, pointer.AsPointer<string>(), context)), inlineAlignment: 0);
    }

    private static StaticDvar? ReadStaticDvarPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, StaticDvar.SerializedSize, "StaticDvar");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, StaticDvar.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var dvar = new StaticDvar
        {
            Dvar = ReadPointer<DvarRuntimeHandle>(rootCursor, context, XPointerResolutionMode.Direct),
            DvarName = ReadXStringPointer(rootCursor, context)
        };

        if (rootCursor.Offset != StaticDvar.SerializedSize)
            throw new InvalidDataException($"StaticDvar consumed 0x{rootCursor.Offset:X} bytes instead of 0x{StaticDvar.SerializedSize:X}.");

        dvar.DvarNameString = context.PointerReader.LoadXString(cursor, dvar.DvarName);
        return dvar;
    }

    private static IReadOnlyList<T> ReadPointerArray<T>(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context,
        string name,
        Func<int, XPointerReference, T> readElement,
        int inlineAlignment)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative pointer-array count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, checked(count * sizeof(int)), name);
            return [];
        }

        AlignStream(cursor, context, 4);
        int tableOffset = cursor.Offset;
        context.Diagnostics.Trace(
            $"              {name} table source=0x{tableOffset:X} count={count} ptr={pointer} blocks={context.Blocks.DescribePositions()}");
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] pointerBytes = context.Blocks.Load(cursor, checked(count * sizeof(int)), out XBlockAddress tableAddress);
        var pointerCursor = new FastFileCursor(pointerBytes, tableAddress);
        var values = new T[count];

        for (int i = 0; i < count; i++)
        {
            XPointerReference elementPointer = context.PointerReader.ReadCell(pointerCursor, XPointerOffsetMode.Direct);
            context.Diagnostics.Trace(
                $"              {name}[{i}] ptr={elementPointer} begin source=0x{cursor.Offset:X} blocks={context.Blocks.DescribePositions()}");
            try
            {
                if (context.PointerReader.HasInlinePayload(elementPointer))
                    context.PointerReader.PatchInlinePointerCell(elementPointer, inlineAlignment);

                values[i] = readElement(i, elementPointer);
            }
            catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException or OverflowException)
            {
                throw new InvalidDataException(
                    $"{name}[{i}] pointer 0x{elementPointer.Raw:X8} from table source 0x{tableOffset:X} failed at cursor 0x{cursor.Offset:X}.",
                    ex);
            }
        }

        return values;
    }

    private static void ReadItemTypeData(
        FastFileCursor cursor,
        ItemDefAsset item,
        FastFileLoadContext context)
    {
        switch (item.Type)
        {
            case ItemDefType.Text:
            case ItemDefType.EditField:
            case ItemDefType.NumericField:
            case ItemDefType.Slider:
            case ItemDefType.YesNo:
            case ItemDefType.Bind:
            case ItemDefType.Validation:
            case ItemDefType.DecimalField:
            case ItemDefType.UpDown:
            case ItemDefType.EmailField:
            case ItemDefType.PassWordField:
                item.EditField = ReadEditFieldDefPointer(cursor, item.TypeData.EditField.Untyped, context);
                break;

            case ItemDefType.ListBox:
                item.ListBox = ReadListBoxDefPointer(cursor, item.TypeData.ListBox.Untyped, context);
                break;

            case ItemDefType.Multi:
                item.Multi = ReadMultiDefPointer(cursor, item.TypeData.Multi.Untyped, context);
                break;

            case ItemDefType.DvarEnum:
                item.DvarEnumName = ReadXString(cursor, item.TypeData.DvarEnumName, context);
                break;

            case ItemDefType.NewsTicker:
                item.NewsTicker = ReadNewsTickerDefPointer(cursor, item.TypeData.NewsTicker.Untyped, context);
                break;

            case ItemDefType.TextScroll:
                item.TextScroll = ReadTextScrollDefPointer(cursor, item.TypeData.TextScroll.Untyped, context);
                break;
        }
    }

    private static EditFieldDef? ReadEditFieldDefPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, EditFieldDef.SerializedSize, "EditFieldDef");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, EditFieldDef.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var edit = new EditFieldDef
        {
            MinVal = ReadSingle(rootCursor),
            MaxVal = ReadSingle(rootCursor),
            DefVal = ReadSingle(rootCursor),
            Range = ReadSingle(rootCursor),
            MaxChars = rootCursor.ReadInt32(),
            MaxCharsGotoNext = rootCursor.ReadInt32(),
            MaxPaintChars = rootCursor.ReadInt32(),
            PaintOffset = rootCursor.ReadInt32()
        };

        if (rootCursor.Offset != EditFieldDef.SerializedSize)
            throw new InvalidDataException($"EditFieldDef consumed 0x{rootCursor.Offset:X} bytes instead of 0x{EditFieldDef.SerializedSize:X}.");

        return edit;
    }

    private static ListBoxDef? ReadListBoxDefPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, ListBoxDef.SerializedSize, "ListBoxDef");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, ListBoxDef.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var listBox = new ListBoxDef
        {
            StartPos = ReadInt32Array(rootCursor, 4),
            EndPos = ReadInt32Array(rootCursor, 4),
            DrawPadding = rootCursor.ReadInt32(),
            ElementWidth = ReadSingle(rootCursor),
            ElementHeight = ReadSingle(rootCursor),
            ElementStyle = rootCursor.ReadInt32(),
            NumColumns = rootCursor.ReadInt32(),
            ColumnInfo = ReadColumnInfoArray(rootCursor, 16),
            DoubleClick = ReadPointer<MenuEventHandlerSet>(rootCursor, context, XPointerResolutionMode.Direct),
            NotSelectable = rootCursor.ReadInt32(),
            NoScrollbars = rootCursor.ReadInt32(),
            UsePaging = rootCursor.ReadInt32(),
            SelectBorder = ReadVec4(rootCursor),
            SelectIcon = ReadPointer<MaterialAsset>(rootCursor, context, XPointerResolutionMode.AliasCell)
        };

        if (rootCursor.Offset != ListBoxDef.SerializedSize)
            throw new InvalidDataException($"ListBoxDef consumed 0x{rootCursor.Offset:X} bytes instead of 0x{ListBoxDef.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"            ListBoxDef root element={listBox.ElementWidth}x{listBox.ElementHeight} numColumns={listBox.NumColumns} " +
            $"doubleClick=0x{listBox.DoubleClick.Raw:X8} selectIcon=0x{listBox.SelectIcon.Raw:X8} blocks={context.Blocks.DescribePositions()}");

        listBox.DoubleClickSet = ReadMenuEventHandlerSetPointer(cursor, listBox.DoubleClick.Untyped, context);
        ReadMaterialPointer(cursor, listBox.SelectIcon.Untyped, context);
        return listBox;
    }

    private static IReadOnlyList<ColumnInfo> ReadColumnInfoArray(FastFileCursor cursor, int count)
    {
        var columns = new ColumnInfo[count];
        for (int i = 0; i < columns.Length; i++)
        {
            columns[i] = new ColumnInfo
            {
                Pos = cursor.ReadInt32(),
                Width = cursor.ReadInt32(),
                MaxChars = cursor.ReadInt32(),
                Alignment = cursor.ReadInt32()
            };
        }

        return columns;
    }

    private static MultiDef? ReadMultiDefPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, MultiDef.SerializedSize, "MultiDef");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, MultiDef.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var multi = new MultiDef
        {
            DvarList = ReadXStringPointerArray(rootCursor, MultiDef.EntryCapacity, context),
            DvarStr = ReadXStringPointerArray(rootCursor, MultiDef.EntryCapacity, context),
            DvarValue = ReadFloatArray(rootCursor, MultiDef.EntryCapacity),
            Count = rootCursor.ReadInt32(),
            StrDef = rootCursor.ReadInt32()
        };

        if (rootCursor.Offset != MultiDef.SerializedSize)
            throw new InvalidDataException($"MultiDef consumed 0x{rootCursor.Offset:X} bytes instead of 0x{MultiDef.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"            MultiDef root count={multi.Count} strDef=0x{multi.StrDef:X8} blocks={context.Blocks.DescribePositions()}");

        var dvarListStrings = new string?[multi.DvarList.Count];
        for (int i = 0; i < multi.DvarList.Count; i++)
            dvarListStrings[i] = context.PointerReader.LoadXString(cursor, multi.DvarList[i]);
        multi.DvarListStrings = dvarListStrings;

        var dvarStrStrings = new string?[multi.DvarStr.Count];
        for (int i = 0; i < multi.DvarStr.Count; i++)
            dvarStrStrings[i] = context.PointerReader.LoadXString(cursor, multi.DvarStr[i]);
        multi.DvarStrStrings = dvarStrStrings;

        return multi;
    }

    private static IReadOnlyList<XPointer<string>> ReadXStringPointerArray(
        FastFileCursor cursor,
        int count,
        FastFileLoadContext context)
    {
        var pointers = new XPointer<string>[count];
        for (int i = 0; i < pointers.Length; i++)
            pointers[i] = ReadXStringPointer(cursor, context);

        return pointers;
    }

    private static IReadOnlyList<float> ReadFloatArray(FastFileCursor cursor, int count)
    {
        var values = new float[count];
        for (int i = 0; i < values.Length; i++)
            values[i] = ReadSingle(cursor);

        return values;
    }

    private static NewsTickerDef? ReadNewsTickerDefPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, NewsTickerDef.SerializedSize, "NewsTickerDef");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, NewsTickerDef.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var newsTicker = new NewsTickerDef
        {
            FeedId = rootCursor.ReadInt32(),
            Speed = rootCursor.ReadInt32(),
            Spacing = rootCursor.ReadInt32(),
            LastTime = rootCursor.ReadInt32(),
            Start = rootCursor.ReadInt32(),
            End = rootCursor.ReadInt32(),
            X = ReadSingle(rootCursor)
        };

        if (rootCursor.Offset != NewsTickerDef.SerializedSize)
            throw new InvalidDataException($"NewsTickerDef consumed 0x{rootCursor.Offset:X} bytes instead of 0x{NewsTickerDef.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"            NewsTickerDef root feedId={newsTicker.FeedId} speed={newsTicker.Speed} spacing={newsTicker.Spacing} blocks={context.Blocks.DescribePositions()}");

        return newsTicker;
    }

    private static TextScrollDef? ReadTextScrollDefPointer(
        FastFileCursor cursor,
        XPointerReference pointer,
        FastFileLoadContext context)
    {
        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, TextScrollDef.SerializedSize, "TextScrollDef");
            return null;
        }

        AlignStream(cursor, context, 4);
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, TextScrollDef.SerializedSize, out XBlockAddress rootAddress);
        var rootCursor = new FastFileCursor(rootBytes, rootAddress);

        var textScroll = new TextScrollDef
        {
            StartTime = rootCursor.ReadInt32()
        };

        if (rootCursor.Offset != TextScrollDef.SerializedSize)
            throw new InvalidDataException($"TextScrollDef consumed 0x{rootCursor.Offset:X} bytes instead of 0x{TextScrollDef.SerializedSize:X}.");

        context.Diagnostics.Trace(
            $"            TextScrollDef root startTime={textScroll.StartTime} blocks={context.Blocks.DescribePositions()}");

        return textScroll;
    }

    private static IReadOnlyList<ItemFloatExpression> ReadItemFloatExpressions(
        FastFileCursor cursor,
        XPointerReference pointer,
        int count,
        FastFileLoadContext context)
    {
        if (count < 0)
            throw new InvalidDataException($"Invalid negative ItemFloatExpression count {count}.");

        if (!context.PointerReader.HasInlinePayload(pointer))
        {
            context.PointerReader.ValidateOffsetPointerRange(pointer, checked(count * ItemFloatExpression.SerializedSize), "ItemFloatExpression[]");
            return [];
        }

        AlignStream(cursor, context, 4);
        context.Diagnostics.Trace(
            $"            ItemFloatExpression table source=0x{cursor.Offset:X} count={count} ptr={pointer} blocks={context.Blocks.DescribePositions()}");
        context.PointerReader.PatchInlinePointerCell(pointer, alignment: 4);
        byte[] rootBytes = context.Blocks.Load(cursor, checked(count * ItemFloatExpression.SerializedSize), out XBlockAddress tableAddress);
        var rootCursor = new FastFileCursor(rootBytes, tableAddress);
        var expressions = new ItemFloatExpression[count];

        for (int i = 0; i < expressions.Length; i++)
        {
            int rowStart = rootCursor.Offset;
            var target = (ItemFloatExpressionTarget)rootCursor.ReadInt32();
            XPointer<Statement> expressionPointer = ReadPointer<Statement>(rootCursor, context, XPointerResolutionMode.Direct);
            expressions[i] = new ItemFloatExpression
            {
                Target = target,
                Expression = expressionPointer
            };

            if (rootCursor.Offset - rowStart != ItemFloatExpression.SerializedSize)
                throw new InvalidDataException($"ItemFloatExpression consumed 0x{rootCursor.Offset - rowStart:X} bytes instead of 0x{ItemFloatExpression.SerializedSize:X}.");

            expressions[i].Statement = ReadStatementPointer(cursor, expressionPointer.Untyped, context);
        }

        return expressions;
    }

    private static void WarnIfUnsupportedInline(
        XPointerReference pointer,
        string fieldName,
        FastFileLoadContext context)
    {
        if (context.PointerReader.HasInlinePayload(pointer))
            context.Diagnostics.Warn($"{fieldName} pointer 0x{pointer.Raw:X8} has inline payload, but that asset-family child loader is not implemented yet.");
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
        return context.PointerReader.ReadPointer<string>(cursor, XPointerResolutionMode.Direct);
    }

    private static XPointer<T> ReadPointer<T>(
        FastFileCursor cursor,
        FastFileLoadContext context,
        XPointerResolutionMode resolutionMode)
    {
        return context.PointerReader.ReadPointer<T>(cursor, resolutionMode);
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

    private static void AlignStream(
        FastFileCursor cursor,
        FastFileLoadContext context,
        int alignment)
    {
        context.Blocks.AlignCurrent(alignment);
    }
}
