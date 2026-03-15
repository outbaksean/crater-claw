using Microsoft.Extensions.Options;

namespace CraterClaw.Core.Tests;

public sealed class ProviderOptionsValidatorTests
{
    [Fact]
    public void Validate_Passes_ForValidOptionsWithMatchingActive()
    {
        var options = new ProviderOptions
        {
            Active = "local",
            Endpoints = new Dictionary<string, ProviderEndpointOptions>
            {
                ["local"] = new() { BaseUrl = "http://localhost:11434" },
                ["remote"] = new() { BaseUrl = "http://192.168.1.1:11434" }
            }
        };

        var result = new ProviderOptionsValidator().Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_Fails_ForInvalidBaseUrl()
    {
        var options = new ProviderOptions
        {
            Endpoints = new Dictionary<string, ProviderEndpointOptions>
            {
                ["local"] = new() { BaseUrl = "not-a-url" }
            }
        };

        var result = new ProviderOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_Fails_WhenActiveNotInEndpoints()
    {
        var options = new ProviderOptions
        {
            Active = "missing",
            Endpoints = new Dictionary<string, ProviderEndpointOptions>
            {
                ["local"] = new() { BaseUrl = "http://localhost:11434" }
            }
        };

        var result = new ProviderOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_Fails_WhenEndpointsIsEmpty()
    {
        var options = new ProviderOptions { Endpoints = [] };

        var result = new ProviderOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
    }
}
