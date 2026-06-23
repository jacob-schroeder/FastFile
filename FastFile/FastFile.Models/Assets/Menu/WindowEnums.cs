namespace FastFile.Models.Assets.Menu;

public enum WindowStyle : int
{
    /// <summary>
    /// Draw no background for the window.
    /// </summary>
    WINDOW_STYLE_EMPTY = 0,

    /// <summary>
    /// Fill the window rectangle with backColor.
    /// </summary>
    WINDOW_STYLE_FILLED = 1,

    /// <summary>
    /// Draw a gradient fill based on backColor.
    /// </summary>
    WINDOW_STYLE_GRADIENT = 2,

    /// <summary>
    /// Draw the background material in the window rectangle.
    /// </summary>
    WINDOW_STYLE_SHADER = 3,

    /// <summary>
    /// Draw using the current team color.
    /// </summary>
    WINDOW_STYLE_TEAMCOLOR = 4,

    /// <summary>
    /// Draw a cinematic/movie-backed window.
    /// </summary>
    WINDOW_STYLE_CINEMATIC = 5
}

public enum WindowBorder : int
{
    /// <summary>
    /// Draw no border.
    /// </summary>
    WINDOW_BORDER_NONE = 0,

    /// <summary>
    /// Draw all four border edges.
    /// </summary>
    WINDOW_BORDER_FULL = 1,

    /// <summary>
    /// Draw only the top and bottom border edges.
    /// </summary>
    WINDOW_BORDER_HORZ = 2,

    /// <summary>
    /// Draw only the left and right border edges.
    /// </summary>
    WINDOW_BORDER_VERT = 3,

    /// <summary>
    /// Draw horizontal gradient-bar borders.
    /// </summary>
    WINDOW_BORDER_KCGRADIENT = 4
}

public enum WindowOwnerDraw : int
{
    None = 0,

    /// <summary>
    /// PS3 selector 0x0FA. Branch tests key-bind state and draws EXE_KEYWAIT or EXE_KEYCHANGE.
    /// Xbox symbol correlation: UI_DrawKeyBindStatus.
    /// </summary>
    UI_OWNERDRAW_KEY_BIND_STATUS = 0x0FA,

    /// <summary>
    /// PS3 selector 0x0FB dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_0FB = 0x0FB,

    /// <summary>
    /// PS3 selector 0x0FC dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_0FC = 0x0FC,

    /// <summary>
    /// PS3 selector 0x0FD dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_0FD = 0x0FD,

    /// <summary>
    /// PS3 selector 0x0FE dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_0FE = 0x0FE,

    /// <summary>
    /// PS3 selector 0x0FF dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_0FF = 0x0FF,

    /// <summary>
    /// PS3 selector 0x100 dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_100 = 0x100,

    /// <summary>
    /// PS3 selector 0x101 dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_101 = 0x101,

    /// <summary>
    /// PS3 selector 0x102 dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_102 = 0x102,

    /// <summary>
    /// PS3 selector 0x103 dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_103 = 0x103,

    /// <summary>
    /// PS3 selector 0x104 dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_104 = 0x104,

    /// <summary>
    /// PS3 selector 0x105 dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_105 = 0x105,

    /// <summary>
    /// PS3 selector 0x106 dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_106 = 0x106,

    /// <summary>
    /// PS3 selector 0x107 dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_107 = 0x107,

    /// <summary>
    /// PS3 selector 0x108 dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_108 = 0x108,

    /// <summary>
    /// PS3 selector 0x109 dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_109 = 0x109,

    /// <summary>
    /// PS3 selector 0x10A. Draws the voice_on material when local talking state is active.
    /// Xbox symbol correlation: UI_DrawLocalTalking.
    /// </summary>
    UI_OWNERDRAW_LOCAL_TALKING = 0x10A,

    /// <summary>
    /// PS3 selector 0x10B. Talker slot 0, using selector - 0x10B.
    /// Xbox symbol correlation: UI_DrawTalkerNum.
    /// </summary>
    UI_OWNERDRAW_TALKER_NUM_0 = 0x10B,

    /// <summary>
    /// PS3 selector 0x10C. Talker slot 1, using selector - 0x10B.
    /// Xbox symbol correlation: UI_DrawTalkerNum.
    /// </summary>
    UI_OWNERDRAW_TALKER_NUM_1 = 0x10C,

    /// <summary>
    /// PS3 selector 0x10D. Talker slot 2, using selector - 0x10B.
    /// Xbox symbol correlation: UI_DrawTalkerNum.
    /// </summary>
    UI_OWNERDRAW_TALKER_NUM_2 = 0x10D,

    /// <summary>
    /// PS3 selector 0x10E. Talker slot 3, using selector - 0x10B.
    /// Xbox symbol correlation: UI_DrawTalkerNum.
    /// </summary>
    UI_OWNERDRAW_TALKER_NUM_3 = 0x10E,

    /// <summary>
    /// PS3 selector 0x10F dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_10F = 0x10F,

    /// <summary>
    /// PS3 selector 0x110. Draws signed-in user text using XBOXLIVE_SIGNEDINAS or XBOXLIVE_NOTSIGNEDIN.
    /// Xbox symbol correlation: UI_DrawLoggedInUser.
    /// </summary>
    UI_OWNERDRAW_LOGGED_IN_USER = 0x110,

    /// <summary>
    /// PS3 selector 0x111. Formats and draws a reserved-slot/count value.
    /// Xbox symbol correlation: UI_DrawReservedSlots.
    /// </summary>
    UI_OWNERDRAW_RESERVED_SLOTS = 0x111,

    /// <summary>
    /// PS3 selector 0x112 dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_112 = 0x112,

    /// <summary>
    /// PS3 selector 0x113. Draws playlist description/population/party-size text.
    /// Xbox symbol correlation: UI_DrawPlaylistDescription.
    /// </summary>
    UI_OWNERDRAW_PLAYLIST_DESCRIPTION = 0x113,

    /// <summary>
    /// PS3 selector 0x114. Draws the logged-in user name, including the [%s]%s formatted path.
    /// Xbox symbol correlation: UI_DrawLoggedInUserName.
    /// </summary>
    UI_OWNERDRAW_LOGGED_IN_USER_NAME = 0x114,

    /// <summary>
    /// PS3 selector 0x115 dispatches directly to the owner-draw epilogue.
    /// </summary>
    UI_OWNERDRAW_NOOP_115 = 0x115,

    /// <summary>
    /// PS3 selector 0x116. Draws map custom data selected by longname, description, or mapimage.
    /// Xbox symbol correlation: UI_DrawMapCustomData.
    /// </summary>
    UI_OWNERDRAW_MAP_CUSTOM_DATA = 0x116
}

[Flags]
public enum WindowStaticFlags : int
{
    None = 0,

    /// <summary>
    /// Tested by Xbox Window_IsDecoration and PS3 UI consumers against window + 0x44.
    /// </summary>
    WINDOW_STATIC_DECORATION = 0x00100000,

    /// <summary>
    /// Tested by Xbox Window_IsHorizontal and PS3 horizontal scroll consumers against window + 0x44.
    /// </summary>
    WINDOW_STATIC_HORIZONTAL = 0x00200000,

    /// <summary>
    /// Tested by Xbox Window_IsScreenSpace. PS3 downstream scans for matching window + 0x44
    /// branch consumers have not identified this mask yet.
    /// </summary>
    WINDOW_STATIC_SCREEN_SPACE = 0x00400000,

    /// <summary>
    /// PS3 item text paint tests this and routes through the wrapped-text rectangle path.
    /// </summary>
    WINDOW_STATIC_AUTOWRAPPED = 0x00800000,

    /// <summary>
    /// PS3 item paint checks this before temporarily applying legacy split-screen placement scale,
    /// then restores the default placement scale after drawing.
    /// </summary>
    WINDOW_STATIC_LEGACY_SPLITSCREEN_SCALE = 0x04000000,

    /// <summary>
    /// PS3 item visibility checks this with local-client runtime state and suppresses drawing while the matching state is active.
    /// </summary>
    WINDOW_STATIC_HIDDEN_DURING_FLASH = 0x10000000,

    /// <summary>
    /// PS3 item visibility checks this with local-client scoped/overlay state and suppresses drawing while the matching state is active.
    /// </summary>
    WINDOW_STATIC_HIDDEN_DURING_SCOPE = 0x20000000,

    /// <summary>
    /// PS3 item visibility checks this against a local-client UI/HUD state bit and suppresses drawing while the matching state is active.
    /// </summary>
    WINDOW_STATIC_HIDDEN_DURING_UI = 0x40000000
}

[Flags]
public enum WindowDynamicFlags : int
{
    None = 0,

    /// <summary>
    /// Tested by Xbox Window_HasFocus and PS3 UI focus loops after visible succeeds.
    /// </summary>
    WINDOW_DYNAMIC_HAS_FOCUS = 0x00000002,

    /// <summary>
    /// Tested by Xbox Window_IsVisible and PS3 UI visibility consumers against dynamicFlags[localClientNum].
    /// </summary>
    WINDOW_DYNAMIC_VISIBLE = 0x00000004,

    /// <summary>
    /// PS3 window/background paint checks this to use window.foreColor instead of the default UI foreground color.
    /// </summary>
    WINDOW_DYNAMIC_HAS_FORECOLOR = 0x00010000
}
