using FastFile.Logic.Zone.Validation;

namespace FastFile.Tests;

public sealed class XStructMetadataValidatorTests
{
    [Fact]
    public void AssetMetadataHasNoStructuralErrors()
    {
        var result = XStructMetadataValidator.ValidateAssetMetadata();

        Assert.True(
            result.IsValid,
            string.Join(Environment.NewLine, result.Errors.Select(error => error.ToString())));
    }
}
