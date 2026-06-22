namespace FastFile.Models.Assets;

public abstract class BaseAsset : IBaseAsset
{
    public int Offset { get; init; }

    protected BaseAsset()
    {
        
    }
}

public interface IBaseAsset
{
    int Offset { get; }
}
