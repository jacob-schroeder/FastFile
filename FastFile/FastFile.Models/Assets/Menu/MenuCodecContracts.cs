using FastFile.Models.Codecs;

namespace FastFile.Models.Assets.Menu;

public static class MenuCodecContracts
{
    public static readonly IReadOnlyList<IXCodecContract> All =
    [
        RectangleDefContract.Contract,
        WindowDefContract.Contract,
        .. MenuFileCodecContracts.All
    ];

    public static void Register(XCodecContractRegistry registry)
    {
        registry.AddRange(All);
    }
}
