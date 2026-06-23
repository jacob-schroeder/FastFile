namespace FastFile.Models.Assets.Menu;

public enum HorizontalAlign : byte
{
    /// <summary>
    /// Anchor X to the left edge of the 4:3 virtual screen, before safe-area adjustment.
    /// This is also the default horizontal rect alignment.
    /// </summary>
    HORIZONTAL_ALIGN_SUBLEFT = 0,

    /// <summary>
    /// Anchor X to the left viewable safe-area edge.
    /// </summary>
    HORIZONTAL_ALIGN_LEFT = 1,

    /// <summary>
    /// Anchor X to the horizontal center of the screen.
    /// </summary>
    HORIZONTAL_ALIGN_CENTER = 2,

    /// <summary>
    /// Anchor X to the right viewable safe-area edge.
    /// The rect offset is applied from the right side, so preview code should subtract width and X.
    /// </summary>
    HORIZONTAL_ALIGN_RIGHT = 3,

    /// <summary>
    /// Use the full horizontal screen span, disregarding safe-area adjustment.
    /// Preview code should treat the resulting X as the screen left and width as the full available width.
    /// </summary>
    HORIZONTAL_ALIGN_FULLSCREEN = 4,

    /// <summary>
    /// Use exact X and width parameters without safe-area adjustment or screen-size scaling.
    /// </summary>
    HORIZONTAL_ALIGN_NOSCALE = 5,

    /// <summary>
    /// Scale a real-screen-resolution X coordinate down into the 0..640 virtual coordinate range.
    /// </summary>
    HORIZONTAL_ALIGN_TO640 = 6,

    /// <summary>
    /// Anchor X to the horizontal center of the safe area.
    /// </summary>
    HORIZONTAL_ALIGN_CENTER_SAFEAREA = 7
}

public enum VerticalAlign : byte
{
    /// <summary>
    /// Anchor Y to the top edge of the 4:3 virtual screen, before safe-area adjustment.
    /// This is also the default vertical rect alignment.
    /// </summary>
    VERTICAL_ALIGN_SUBTOP = 0,

    /// <summary>
    /// Anchor Y to the top viewable safe-area edge.
    /// </summary>
    VERTICAL_ALIGN_TOP = 1,

    /// <summary>
    /// Anchor Y to the vertical center of the screen.
    /// </summary>
    VERTICAL_ALIGN_CENTER = 2,

    /// <summary>
    /// Anchor Y to the bottom viewable safe-area edge.
    /// The rect offset is applied from the bottom side, so preview code should subtract height and Y.
    /// </summary>
    VERTICAL_ALIGN_BOTTOM = 3,

    /// <summary>
    /// Use the full vertical screen span, disregarding safe-area adjustment.
    /// Preview code should treat the resulting Y as the screen top and height as the full available height.
    /// </summary>
    VERTICAL_ALIGN_FULLSCREEN = 4,

    /// <summary>
    /// Use exact Y and height parameters without safe-area adjustment or screen-size scaling.
    /// </summary>
    VERTICAL_ALIGN_NOSCALE = 5,

    /// <summary>
    /// Scale a real-screen-resolution Y coordinate down into the 0..480 virtual coordinate range.
    /// </summary>
    VERTICAL_ALIGN_TO480 = 6,

    /// <summary>
    /// Anchor Y to the vertical center of the safe area.
    /// </summary>
    VERTICAL_ALIGN_CENTER_SAFEAREA = 7
}
