using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CraterClaw.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CraterClaw.Api.Tests;

public sealed class ProvidersEndpointTests
{
    [Fact]
    public async Task GetProviders_ReturnsConfiguredEndpoints()
    {
        using var factory = new CraterClawApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/providers");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var endpoints = body.EnumerateArray().ToList();
        Assert.NotEmpty(endpoints);
        var testEndpoint = endpoints.Single(e => e.GetProperty("name").GetString() == "test");
        Assert.Equal("http://localhost:11434", testEndpoint.GetProperty("baseUrl").GetString());
    }

    [Fact]
    public async Task GetProviderStatus_KnownEndpoint_ReturnsStatus()
    {
        var fake = new FakeProviderStatusService(new ProviderStatus(true, null));
        using var factory = new CraterClawApiFactory(
            services => services.AddSingleton<IProviderStatusService>(fake));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/providers/test/status");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("isReachable").GetBoolean());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("errorMessage").ValueKind);
    }

    [Fact]
    public async Task GetProviderStatus_UnreachableEndpoint_ReturnsStatus()
    {
        var fake = new FakeProviderStatusService(new ProviderStatus(false, "Connection refused"));
        using var factory = new CraterClawApiFactory(
            services => services.AddSingleton<IProviderStatusService>(fake));
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/providers/test/status");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("isReachable").GetBoolean());
        Assert.Equal("Connection refused", body.GetProperty("errorMessage").GetString());
    }

    [Fact]
    public async Task GetProviderStatus_UnknownEndpoint_Returns404()
    {
        using var factory = new CraterClawApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/providers/unknown/status");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
