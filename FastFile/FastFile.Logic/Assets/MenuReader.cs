using FastFile.Logic.Extensions;
using FastFile.Models.Assets.Material;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Data;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menu.Elements;

namespace FastFile.Logic.Assets;

// We're going to need a bigger class... (jaws reference)
public static class MenuReader
{
    public static MenuDef Read(ReadOnlySpan<byte> span, ref int position)
    {
        var asset = new MenuDef()
        {
            Offset = position
        };

        asset.Window = ReadWindow(span, ref position);

        position += 4; //I'm not sure what's happening here.
        
        asset.FontPtr = Memory.ReadPointer<string>(span, ref position);
        asset.Fullscreen = span.ReadInt32(ref position);
        asset.ItemCount = span.ReadInt32(ref position);
        asset.FontIndex = span.ReadInt32(ref position);

        for (int i = 0; i < asset.CursorItems.Length; i++)
            asset.CursorItems[i] = span.ReadInt32(ref position);
        
        asset.FadeCycle = span.ReadInt32(ref position);
        asset.FadeClamp = span.ReadFloat(ref position);
        asset.FadeAmount = span.ReadFloat(ref position);
        asset.FadeInAmount = span.ReadFloat(ref position);
        asset.BlurRadius = span.ReadFloat(ref position);

        asset.OnOpen = Memory.ReadPointer<MenuEventHandlerSet>(span, ref position);
        asset.OnRequestClose = Memory.ReadPointer<MenuEventHandlerSet>(span, ref position);
        asset.OnClose = Memory.ReadPointer<MenuEventHandlerSet>(span, ref position);
        asset.OnEsc = Memory.ReadPointer<MenuEventHandlerSet>(span, ref position);
        
        asset.ExecKeys = Memory.ReadPointer<ItemKeyHandler>(span, ref position);
        asset.VisibleExp = Memory.ReadPointer<Statement_s>(span, ref position);
        asset.AllowedBinding = Memory.ReadPointer<string>(span, ref position);
        asset.SoundName = Memory.ReadPointer<string>(span, ref position);

        asset.ImageTrack = span.ReadInt32(ref position);
        asset.FocusColor = span.ReadVec4(ref position);
        
        asset.RectXExp = Memory.ReadPointer<Statement_s>(span, ref position);
        asset.RectYExp = Memory.ReadPointer<Statement_s>(span, ref position);
        asset.RectHExp = Memory.ReadPointer<Statement_s>(span, ref position);
        asset.RectWExp = Memory.ReadPointer<Statement_s>(span, ref position);
        
        #if PC
        
        #else
        asset.Items = Memory.ReadPointer<ZonePointer<ItemDef_s>[]>(span, ref position);
        #endif
        
        return asset;
    }

    private static Window ReadWindow(ReadOnlySpan<byte> span, ref int position)
    {
        var window = new Window();

        window.NamePtr = Memory.ReadPointer<string>(span, ref position);

        window.Rect = new RectangleDef
        {
            X = span.ReadFloat(ref position),
            Y = span.ReadFloat(ref position),
            W = span.ReadFloat(ref position),
            H = span.ReadFloat(ref position),
            HorzAlign = span.ReadByte(ref position),
            VertAlign = span.ReadByte(ref position)
        };
        
        window.RectClient = new RectangleDef
        {
            X = span.ReadFloat(ref position),
            Y = span.ReadFloat(ref position),
            W = span.ReadFloat(ref position),
            H = span.ReadFloat(ref position),
            HorzAlign = span.ReadByte(ref position),
            VertAlign = span.ReadByte(ref position)
        };

        window.GroupPtr = Memory.ReadPointer<string>(span, ref position);

        window.Style = span.ReadInt32(ref position);
        window.Border = span.ReadInt32(ref position);
        window.OwnerDraw = span.ReadInt32(ref position);
        window.OwnerDrawFlags = span.ReadInt32(ref position);
        window.BorderSize = span.ReadFloat(ref position);
        window.StaticFlags = span.ReadInt32(ref position);

        for (int i = 0; i < window.DynamicFlags.Length; i++)
        {
            window.DynamicFlags[i] = span.ReadInt32(ref position);
        }
        
        window.NextTime = span.ReadInt32(ref position);
        
        window.foreColor = span.ReadVec4(ref position);
        window.backColor = span.ReadVec4(ref position);
        window.borderColor = span.ReadVec4(ref position);
        window.outlineColor = span.ReadVec4(ref position);
        window.disableColor = span.ReadVec4(ref position);
        
        window.MaterialPtr = Memory.ReadPointer<Material>(span, ref position);
        
        ResolveMaterial(window.MaterialPtr, span,  ref position);
        
        return window;
    }

    private static void ResolveMaterial(ZonePointer<Material> Material, ReadOnlySpan<byte> span, ref int position)
    {
        if (Material.Kind == PointerKind.Null)
            return;
        
        //Material is like this huge asset that I'll need to struct out.
        throw new NotImplementedException();
    }
}