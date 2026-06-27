using FastFile.Models.Assets;
using FastFile.Models.Codecs;

namespace FastFile.Emitters;

public interface IXAssetEmitter<in TAsset>
    where TAsset : BaseAsset
{
    IXAssetCodecContract Contract { get; }
    void EmitAsset(XEmitContext context, TAsset asset);
}

public interface IXStructEmitter<in T>
{
    IXCodecContract Contract { get; }
    void EmitStruct(XEmitContext context, T value);
}
