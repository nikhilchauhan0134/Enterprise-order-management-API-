using SOPS.Application.Options;

namespace SOPS.Tests;

public sealed class ThirdPartyVersionTests
{
    [Theory]
    [InlineData("v1", "v1")]
    [InlineData("V1", "v1")]
    [InlineData("v2", "v2")]
    [InlineData("V2", "v2")]
    [InlineData("anything", "v1")]
    public void NormalizeVersion_Should_Map_Correctly(string input, string expected) =>
        Assert.Equal(expected, ThirdPartyOptions.NormalizeVersion(input));

    [Theory]
    [InlineData("1.0", "v1")]
    [InlineData("2.0", "v2")]
    [InlineData(null, "v1")]
    public void MapFromSopsApiVersion_Should_Map_Automatically(string? sopsApi, string expected)
    {
        var options = new ThirdPartyOptions { ActiveVersion = "v1" };
        Assert.Equal(expected, options.MapFromSopsApiVersion(sopsApi));
    }

    [Fact]
    public void ResolveVersion_Should_ReturnV2_Config()
    {
        var options = new ThirdPartyOptions
        {
            V1 = new ThirdPartyVersionOptions { BaseUrl = "https://v1.test/" },
            V2 = new ThirdPartyVersionOptions { BaseUrl = "https://v2.test/", SubmitOrderPath = "/api/v2/orders" }
        };

        var v2 = options.ResolveVersion("v2");
        Assert.Equal("https://v2.test/", v2.BaseUrl);
        Assert.Equal("/api/v2/orders", v2.SubmitOrderPath);
    }
}
