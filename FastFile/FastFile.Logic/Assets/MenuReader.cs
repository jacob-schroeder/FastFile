using FastFile.Logic.Extensions;
using FastFile.Models.Assets.Menufile;
using FastFile.Models.Data;
using FastFile.Models.Assets.Menu;
using FastFile.Models.Assets.Menu.Elements;

namespace FastFile.Logic.Assets;

public static class MenuReader
{
    public static MenuDef Read(ReadOnlySpan<byte> span, ref int position)
    {
        var asset = new MenuDef()
        {
            Offset = position,
            
            Window = ReadWindow(span, ref position),
            FontPtr = Memory.ReadPointer<string>(span, ref position),
            fullscreen = span.ReadInt32(ref position),
            itemCount = span.ReadInt32(ref position),
            fontIndex = span.ReadInt32(ref position),
            
        };
        
        return asset;
    }

    public static Window ReadWindow(ReadOnlySpan<byte> span, ref int position)
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
        
        window.MaterialPtr = Memory.ReadPointer<int>(span, ref position);
        
        //ResolveMaterial(window.MaterialPtr.Result, span,  ref position);
        
        return window;
    }

    public static void ResolveMaterial(ref int Material, ReadOnlySpan<byte> span, ref int position)
    {
        throw new NotImplementedException();
    }
}
