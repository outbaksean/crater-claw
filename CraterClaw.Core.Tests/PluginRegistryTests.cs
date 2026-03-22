using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;

namespace CraterClaw.Core.Tests;

public sealed class PluginRegistryTests
{
    private static IPluginRegistry CreateRegistry() =>
        new DefaultPluginRegistry(
            new Dictionary<string, Func<IReadOnlyDictionary<string, string>, object>>
            {
                ["qbittorrent"] = config => new QBitTorrentPlugin(
                    new HttpClient(),
                    new QBitTorrentOptions
                    {
                        BaseUrl = config.GetValueOrDefault("baseUrl"),
                        Username = config.GetValueOrDefault("username"),
                        Password = config.GetValueOrDefault("password")
                    },
                    NullLogger<QBitTorrentPlugin>.Instance)
            },
            NullLogger<DefaultPluginRegistry>.Instance);

    private static PluginBinding Binding(string name, IReadOnlyList<string> tools) =>
        new(name, tools, new Dictionary<string, string>());

    [Fact]
    public void Resolve_QbittorrentWithEmptyTools_ReturnsAllFunctions()
    {
        var registry = CreateRegistry();

        var plugins = registry.Resolve([Binding("qbittorrent", [])]);

        Assert.Single(plugins);
        Assert.Equal(7, plugins[0].Count());
    }

    [Fact]
    public void Resolve_QbittorrentWithToolFilter_ReturnsOnlyRequestedFunction()
    {
        var registry = CreateRegistry();

        var plugins = registry.Resolve([Binding("qbittorrent", ["ListTorrents"])]);

        Assert.Single(plugins);
        var functions = plugins[0].ToList();
        Assert.Single(functions);
        Assert.Equal("ListTorrents", functions[0].Name);
    }

    [Fact]
    public void Resolve_WithUnknownToolName_SkipsUnknown_KnownFunctionsIncluded()
    {
        var registry = CreateRegistry();

        var plugins = registry.Resolve([Binding("qbittorrent", ["ListTorrents", "DoesNotExist"])]);

        Assert.Single(plugins);
        var functions = plugins[0].ToList();
        Assert.Single(functions);
        Assert.Equal("ListTorrents", functions[0].Name);
    }

    [Fact]
    public void Resolve_EmptyBindingList_ReturnsEmptyList()
    {
        var registry = CreateRegistry();

        var plugins = registry.Resolve([]);

        Assert.Empty(plugins);
    }

    [Fact]
    public void Resolve_UnknownPluginName_ReturnsEmptyList()
    {
        var registry = CreateRegistry();

        var plugins = registry.Resolve([Binding("unknown-plugin", [])]);

        Assert.Empty(plugins);
    }

    [Fact]
    public void Resolve_MixOfKnownAndUnknown_ReturnsOnlyKnown()
    {
        var registry = CreateRegistry();

        var plugins = registry.Resolve([Binding("qbittorrent", []), Binding("unknown-plugin", [])]);

        Assert.Single(plugins);
    }

    [Fact]
    public void Resolve_PassesConfigToFactory()
    {
        IReadOnlyDictionary<string, string>? capturedConfig = null;
        var registry = new DefaultPluginRegistry(
            new Dictionary<string, Func<IReadOnlyDictionary<string, string>, object>>
            {
                ["qbittorrent"] = config =>
                {
                    capturedConfig = config;
                    return new QBitTorrentPlugin(
                        new HttpClient(),
                        new QBitTorrentOptions(),
                        NullLogger<QBitTorrentPlugin>.Instance);
                }
            },
            NullLogger<DefaultPluginRegistry>.Instance);
        var binding = new PluginBinding("qbittorrent", [],
            new Dictionary<string, string> { ["baseUrl"] = "http://localhost:8080", ["username"] = "admin" });

        registry.Resolve([binding]);

        Assert.NotNull(capturedConfig);
        Assert.Equal("http://localhost:8080", capturedConfig["baseUrl"]);
        Assert.Equal("admin", capturedConfig["username"]);
    }
}
