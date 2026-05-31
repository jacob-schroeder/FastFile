using FastFile.Models.Data;
using FastFile.Models.Zone;

namespace FastFile.Models.Assets.Effects;

public class FxEffectDef() : BaseAsset(XAssetType.Fx)
{
    public ZonePointer<string> NamePtr { get; set; }
    public string Name => NamePtr is { IsResolved: true } ? NamePtr.Result ?? string.Empty : string.Empty;
    public int Flags { get; set; }
    public int TotalSize { get; set; }
    public int MsecLoopingLife { get; set; }
    public int ElemDefCountLooping { get; set; }
    public int ElemDefCountOneShot { get; set; }
    public int ElemDefCountEmission { get; set; }
    public ZonePointer<FxElemDef[]> ElemDefs { get; set; }

    public override string? GetDisplayName => Name;
}

public sealed class FxElemDef
{
    public int Flags { get; set; }
    public byte ElemType { get; set; }
    public byte VisualCount { get; set; }
    public byte VelIntervalCount { get; set; }
    public byte VisStateIntervalCount { get; set; }
    public ZonePointer<byte[]> VelSamples { get; set; }
    public ZonePointer<byte[]> VisSamples { get; set; }
    public ZonePointer<byte> Visuals { get; set; }
    public ZonePointer<byte> EffectOnImpact { get; set; }
    public ZonePointer<byte> EffectOnDeath { get; set; }
    public ZonePointer<byte> EffectEmitted { get; set; }
    public ZonePointer<byte> Extended { get; set; }
}
