using FastFile.Models.Assets.SoundAliasList;
using FastFile.Models.Data;

namespace FastFile.Models.Assets.Menu.Elements;

public struct ItemDef_s
{
    public Window window;
#if !PC
    public RectangleDef[] textRect = new RectangleDef[4];
#else
    public RectangleDef textRect = new RectangleDef[1];
#endif
    public int type;
    public int dataType;
    public int align;
    public int fontEnum;
    public int textAlignMode;
    public float textAlignX;
    public float textAlignY;
    public float textScale;
    public int textStyle;
    public int gameMsgWindowIndex;
    public int gameMsgWindowMode;
    public ZonePointer<string> text;
    public int textSaveGameInfo;
    public ZonePointer<MenuDef> parent;
    public ZonePointer<MenuEventHandlerSet> mouseEnterText;
    public ZonePointer<MenuEventHandlerSet> mouseExitText;
    public ZonePointer<MenuEventHandlerSet> mouseEnter;
    public ZonePointer<MenuEventHandlerSet> mouseExit;
    public ZonePointer<MenuEventHandlerSet> action;
    public ZonePointer<MenuEventHandlerSet> accept;
    public ZonePointer<MenuEventHandlerSet> onFocus;
    public ZonePointer<MenuEventHandlerSet> leaveFocus;
    public ZonePointer<string> dvar;
    public ZonePointer<string> dvarTest;
    public ZonePointer<ItemKeyHandler> onKey;
    public ZonePointer<string> enableDvar;
    public ZonePointer<int> dvarFlags;
    public ZonePointer<snd_alias_list_t> focusSound;
    public float special;
#if !PC
    public int[] cursorPos = new int[4];
#else
    public int[] cursorPos = new int[1];
#endif
    public itemDefData_t typeData;
    public int imageTrack;
    public int floatExpressionCount;
    public ZonePointer<ItemFloatExpression> floatExpressions;
    public ZonePointer<Statement_s> visibleExp;
    public ZonePointer<Statement_s> disabledExp;
    public ZonePointer<Statement_s> textExp;
    public ZonePointer<Statement_s> materialExp;
    public Vec4 glowColor;
    public bool decayActive;
    public int fxBirthTime;
    public int fxLetterTime;
    public int fxDecayStartTime;
    public int fxDecayDuration;
    public int lastSoundPlayedTime;

    public ItemDef_s()
    {
        unsafe
        {
            window = null;
            type = 0;
            dataType = 0;
            align = 0;
            fontEnum = 0;
            textAlignMode = 0;
            textAlignX = 0;
            textAlignY = 0;
            textScale = 0;
            textStyle = 0;
            gameMsgWindowIndex = 0;
            gameMsgWindowMode = 0;
            text = null;
            textSaveGameInfo = 0;
            parent = null;
            mouseEnterText = null;
            mouseExitText = null;
            mouseEnter = null;
            mouseExit = null;
            action = null;
            accept = null;
            onFocus = null;
            leaveFocus = null;
            dvar = null;
            dvarTest = null;
            onKey = null;
            enableDvar = null;
            dvarFlags = null;
            focusSound = null;
            special = 0;
            cursorPos = null;
            typeData = default;
            imageTrack = 0;
            floatExpressionCount = 0;
            floatExpressions = null;
            visibleExp = null;
            disabledExp = null;
            textExp = null;
            materialExp = null;
            glowColor = default;
            decayActive = false;
            fxBirthTime = 0;
            fxLetterTime = 0;
            fxDecayStartTime = 0;
            fxDecayDuration = 0;
            lastSoundPlayedTime = 0;
        }
    }
}