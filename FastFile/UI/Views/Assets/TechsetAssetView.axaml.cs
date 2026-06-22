using Avalonia.Controls;
using Avalonia.Interactivity;
using FastFile.ModelsOLD.Assets.TechniqueSet;
using FastFile.ModelsOLD.Data;
using UI.Models;
using System.Globalization;
using System.Linq;
using FastFile.ModelsOLD.Zone;
using UI.Navigation;

namespace UI.Views.Assets;

public partial class TechsetAssetView : UserControl
{
    public TechsetAssetView()
    {
        InitializeComponent();
    }

    public TechsetAssetView(MaterialTechniqueSet techset) : this()
    {
        var displayName = GetDisplayName(techset);
        TechsetNameTextBlock.Text = displayName;
        TechsetSummaryTextBlock.Text = $"World Vertex Format: {techset.WorldVertexFormat}";

        var techniques = BuildTechniques(techset).ToArray();
        TechsetTechniqueCountTextBlock.Text = $"{techniques.Length:N0} techniques";
        TechsetSummaryItemsControl.ItemsSource = BuildSummaryItems(techset, techniques.Length);
        TechsetTechniquesItemsControl.ItemsSource = techniques;
        TechsetTechniquesEmptyTextBlock.IsVisible = techniques.Length == 0;
    }

    private void BlockStreamNavigationButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: BlockStreamNavigationTarget target } button)
        {
            BlockStreamNavigator.Navigate(button, target);
        }
    }

    private static string GetDisplayName(MaterialTechniqueSet techset)
    {
        return string.IsNullOrWhiteSpace(techset.GetDisplayName)
            ? "(unnamed techset)"
            : techset.GetDisplayName;
    }

    private static KeyValueListItem[] BuildSummaryItems(MaterialTechniqueSet techset, int resolvedTechniqueCount)
    {
        return
        [
            new("Display Name", GetDisplayName(techset)),
            new("Offset", $"0x{techset.Offset:X8}"),
            AssetViewFormatters.PointerItem("Name Pointer", techset.NamePtr),
            new("World Vertex Format", techset.WorldVertexFormat.ToString()),
            new("Has Been Uploaded", techset.HasBeenUploaded ? "Yes" : "No"),
            new("Technique Slots", $"{resolvedTechniqueCount:N0} resolved of {techset.Techniques.Length:N0}")
        ];
    }

    private static TechsetTechniqueDisplayItem[] BuildTechniques(MaterialTechniqueSet techset)
    {
        return techset.Techniques
            .Select((pointer, index) => BuildTechniqueItem(pointer, index))
            .Where(item => item is not null)
            .ToArray()!;
    }

    private static TechsetTechniqueDisplayItem? BuildTechniqueItem(
        XPointer<MaterialTechnique>? pointer,
        int index)
    {
        if (pointer is null || pointer.Kind == PointerKind.Null)
        {
            return null;
        }

        if (pointer is { IsResolved: true, Value: { } technique })
        {
            return new TechsetTechniqueDisplayItem(
                slotText: $"Slot {index + 1}",
                name: string.IsNullOrWhiteSpace(technique.Name)
                    ? $"Slot {index + 1} (unnamed)"
                    : technique.Name,
                pointer: AssetViewFormatters.FormatPointerRaw(pointer),
                flags: $"0x{technique.Flags:X4}",
                passCount: technique.PassCount.ToString(CultureInfo.CurrentCulture),
                passesIndicator: $"{technique.Passes.Length:N0} resolved pass entries",
                passes: BuildPasses(technique),
                pointerNavigationTarget: AssetViewFormatters.GetNavigationTarget(pointer));
        }

        return new TechsetTechniqueDisplayItem(
            slotText: $"Slot {index + 1}",
            name: $"Slot {index + 1} (unresolved)",
            pointer: AssetViewFormatters.FormatPointerRaw(pointer),
            flags: "n/a",
            passCount: "n/a",
            passesIndicator: "not resolved",
            passes: [],
            pointerNavigationTarget: AssetViewFormatters.GetNavigationTarget(pointer));
    }

    private static TechsetPassDisplayItem[] BuildPasses(MaterialTechnique technique)
    {
        return technique.Passes
            .Select((pass, index) => BuildPassItem(pass, index))
            .ToArray();
    }

    private static TechsetPassDisplayItem BuildPassItem(MaterialPass pass, int index)
    {
        return new TechsetPassDisplayItem(
            indexText: $"Pass #{index + 1}",
            vertexDeclaration: AssetViewFormatters.FormatPointerRaw(pass.VertexDecl),
            vertexShader: AssetViewFormatters.FormatPointerRaw(pass.VertexShader),
            pixelShader: AssetViewFormatters.FormatPointerRaw(pass.PixelShader),
            argumentSummary: $"{pass.PerPrimArgCount:N0} primitive / {pass.PerObjArgCount:N0} object / {pass.StableArgCount:N0} stable",
            arguments: $"{pass.ArgCount:N0} total args ({AssetViewFormatters.FormatPointerRaw(pass.Args)})",
            flagsAndPrecompiled: $"Flags 0x{pass.CustomSamplerFlags:X2} • Precompiled 0x{pass.PrecompiledIndex:X2}",
            vertexDeclarationNavigationTarget: AssetViewFormatters.GetNavigationTarget(pass.VertexDecl),
            vertexShaderNavigationTarget: AssetViewFormatters.GetNavigationTarget(pass.VertexShader),
            pixelShaderNavigationTarget: AssetViewFormatters.GetNavigationTarget(pass.PixelShader),
            argumentsNavigationTarget: AssetViewFormatters.GetNavigationTarget(pass.Args)
        );
    }
}
