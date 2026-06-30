using FastFile.Models.Zone;

namespace FastFile.Models.Assets;

public abstract class BaseAsset : IBaseAsset
{
    public int Offset { get; init; }
    public XBlockAddress? RuntimeAddress { get; init; }

    protected BaseAsset()
    {
        
    }
}

public interface IBaseAsset
{
    int Offset { get; }
    XBlockAddress? RuntimeAddress { get; }
}
