using System.Net.Http.Json;
using System.Text.Json;

namespace CraterClaw.Api.Tests;

public sealed class ConfigOverrideTests : IDisposable
{
    private readonly string _tempConfigPath;

    public ConfigOverrideTests()
    {
        _tempConfigPath = Path.GetTempFileName();
        File.WriteAllText(_tempConfigPath, """
            {
                "providers": {
                    "active": "override-provider",
                    "endpoints": {
                        "override-provider": { "baseUrl": "http://override-host:11434" }
                    }
                }
            }
            """);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CRATERCLAW_CONFIG", null);
        if (File.Exists(_tempConfigPath))
            File.Delete(_tempConfigPath);
    }

    [Fact]
    public async Task Config_override_via_env_var_loads_specified_file()
    {
        Environment.SetEnvironmentVariable("CRATERCLAW_CONFIG", _tempConfigPath);
        using var factory = new CraterClawApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/providers");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var names = body.EnumerateArray()
            .Select(e => e.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("override-provider", names);
    }
}
