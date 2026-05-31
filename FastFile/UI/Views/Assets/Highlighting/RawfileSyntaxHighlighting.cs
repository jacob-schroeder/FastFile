using System.IO;
using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace UI.Views.Assets.Highlighting;

internal static class RawfileSyntaxHighlighting
{
    public static IHighlightingDefinition Definition => ScriptDefinition;

    private static IHighlightingDefinition ScriptDefinition { get; } = LoadDefinition(ScriptSyntaxDefinition);
    private static IHighlightingDefinition ArenaDefinition { get; } = LoadDefinition(ArenaSyntaxDefinition);
    private static IHighlightingDefinition VisionDefinition { get; } = LoadDefinition(VisionSyntaxDefinition);

    public static IHighlightingDefinition GetDefinition(string? fileName)
    {
        return Path.GetExtension(fileName)?.ToLowerInvariant() switch
        {
            ".arena" => ArenaDefinition,
            ".vision" => VisionDefinition,
            ".csc" => ScriptDefinition,
            ".gsc" => ScriptDefinition,
            _ => ScriptDefinition
        };
    }

    private static IHighlightingDefinition LoadDefinition(string syntaxDefinition)
    {
        using var stringReader = new StringReader(syntaxDefinition);
        using var xmlReader = XmlReader.Create(stringReader);

        return HighlightingLoader.Load(xmlReader, HighlightingManager.Instance);
    }

    private const string ScriptSyntaxDefinition = """
<?xml version="1.0"?>
<SyntaxDefinition name="RawFile Script"
                  extensions=".gsc;.csc"
                  xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
    <Color name="Comment" foreground="#6A9955" />
    <Color name="Preprocessor" foreground="#C586C0" fontWeight="bold" />
    <Color name="Path" foreground="#4EC9B0" />
    <Color name="String" foreground="#CE9178" />
    <Color name="Escape" foreground="#D7BA7D" fontWeight="bold" />
    <Color name="Keyword" foreground="#569CD6" fontWeight="bold" />
    <Color name="Constant" foreground="#4FC1FF" />
    <Color name="Function" foreground="#DCDCAA" />
    <Color name="Property" foreground="#9CDCFE" />
    <Color name="Number" foreground="#B5CEA8" />
    <Color name="Operator" foreground="#D4D4D4" />

    <RuleSet>
        <Span color="Comment" begin="//" />
        <Span color="Comment" multiline="true" begin="/\*" end="\*/" />
        <Span color="Comment" multiline="true" begin="/#" end="#/" />

        <Span color="String" begin="&quot;" end="&quot;">
            <RuleSet>
                <Span color="Escape" begin="\\" end="." />
            </RuleSet>
        </Span>

        <Span color="String" begin="'" end="'">
            <RuleSet>
                <Span color="Escape" begin="\\" end="." />
            </RuleSet>
        </Span>

        <Span color="Preprocessor" begin="#[A-Za-z_][A-Za-z0-9_]*" />
        <Rule color="Path">\b[A-Za-z_][A-Za-z0-9_]*(\\[A-Za-z_][A-Za-z0-9_]*)+(::[A-Za-z_][A-Za-z0-9_]*)?</Rule>
        <Rule color="Function">\b[A-Za-z_][A-Za-z0-9_]*(?=\s*\()</Rule>
        <Rule color="Property">(?&lt;=\.|::)[A-Za-z_][A-Za-z0-9_]*</Rule>
        <Rule color="Number">\b0x[0-9A-Fa-f]+\b|\b\d+(\.\d+)?\b</Rule>
        <Rule color="Operator">==|!=|&lt;=|&gt;=|&amp;&amp;|\|\||\+\+|--|[+\-*/%=&lt;&gt;!&amp;|?:]</Rule>

        <Keywords color="Keyword">
            <Word>break</Word>
            <Word>case</Word>
            <Word>continue</Word>
            <Word>default</Word>
            <Word>else</Word>
            <Word>for</Word>
            <Word>foreach</Word>
            <Word>if</Word>
            <Word>in</Word>
            <Word>return</Word>
            <Word>switch</Word>
            <Word>thread</Word>
            <Word>wait</Word>
            <Word>waittill</Word>
            <Word>while</Word>
        </Keywords>

        <Keywords color="Constant">
            <Word>false</Word>
            <Word>level</Word>
            <Word>self</Word>
            <Word>true</Word>
            <Word>undefined</Word>
        </Keywords>
    </RuleSet>
</SyntaxDefinition>
""";

    private const string ArenaSyntaxDefinition = """
<?xml version="1.0"?>
<SyntaxDefinition name="Arena"
                  extensions=".arena"
                  xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
    <Color name="Comment" foreground="#6A9955" />
    <Color name="Key" foreground="#9CDCFE" fontWeight="bold" />
    <Color name="String" foreground="#CE9178" />
    <Color name="Escape" foreground="#D7BA7D" fontWeight="bold" />
    <Color name="Brace" foreground="#D4D4D4" fontWeight="bold" />
    <Color name="Number" foreground="#B5CEA8" />

    <RuleSet>
        <Span color="Comment" begin="//" />
        <Span color="Comment" multiline="true" begin="/\*" end="\*/" />

        <Span color="String" begin="&quot;" end="&quot;">
            <RuleSet>
                <Span color="Escape" begin="\\" end="." />
            </RuleSet>
        </Span>

        <Span color="String" begin="'" end="'">
            <RuleSet>
                <Span color="Escape" begin="\\" end="." />
            </RuleSet>
        </Span>

        <Rule color="Brace">[\{\}]</Rule>
        <Rule color="Number">\b-?\d+(\.\d+)?\b</Rule>

        <Keywords color="Key">
            <Word>allieschar</Word>
            <Word>axischar</Word>
            <Word>description</Word>
            <Word>environment</Word>
            <Word>gametype</Word>
            <Word>longname</Word>
            <Word>map</Word>
            <Word>mapimage</Word>
            <Word>mapoverlay</Word>
        </Keywords>
    </RuleSet>
</SyntaxDefinition>
""";

    private const string VisionSyntaxDefinition = """
<?xml version="1.0"?>
<SyntaxDefinition name="Vision"
                  extensions=".vision"
                  xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
    <Color name="Comment" foreground="#6A9955" />
    <Color name="Key" foreground="#9CDCFE" fontWeight="bold" />
    <Color name="String" foreground="#CE9178" />
    <Color name="Escape" foreground="#D7BA7D" fontWeight="bold" />
    <Color name="Number" foreground="#B5CEA8" />

    <RuleSet>
        <Span color="Comment" begin="//" />
        <Span color="Comment" multiline="true" begin="/\*" end="\*/" />

        <Span color="String" begin="&quot;" end="&quot;">
            <RuleSet>
                <Span color="Escape" begin="\\" end="." />
                <Rule color="Number">\b-?\d+(\.\d+)?\b</Rule>
            </RuleSet>
        </Span>

        <Span color="String" begin="'" end="'">
            <RuleSet>
                <Span color="Escape" begin="\\" end="." />
                <Rule color="Number">\b-?\d+(\.\d+)?\b</Rule>
            </RuleSet>
        </Span>

        <Rule color="Number">\b-?\d+(\.\d+)?\b</Rule>
        <Rule color="Key">\br_[A-Za-z0-9_]+\b</Rule>
    </RuleSet>
</SyntaxDefinition>
""";
}
