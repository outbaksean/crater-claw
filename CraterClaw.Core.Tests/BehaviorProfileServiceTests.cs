using Microsoft.Extensions.Options;

namespace CraterClaw.Core.Tests;

public sealed class BehaviorProfileServiceTests
{
    private static IBehaviorProfileService CreateService(Dictionary<string, BehaviorEntry> entries) =>
        new BehaviorProfileService(Options.Create(entries));

    [Fact]
    public void GetAll_WithTwoEntries_ReturnsBothProfiles()
    {
        var service = CreateService(new Dictionary<string, BehaviorEntry>
        {
            ["alpha"] = new BehaviorEntry { Name = "Alpha", Description = "First", SystemPrompt = "Prompt A" },
            ["beta"]  = new BehaviorEntry { Name = "Beta",  Description = "Second", SystemPrompt = "Prompt B" }
        });

        var profiles = service.GetAll();

        Assert.Equal(2, profiles.Count);
    }

    [Fact]
    public void GetById_ReturnsMatchingProfile()
    {
        var service = CreateService(new Dictionary<string, BehaviorEntry>
        {
            ["alpha"] = new BehaviorEntry { Name = "Alpha", Description = "First", SystemPrompt = "Prompt A" }
        });

        var profile = service.GetById("alpha");

        Assert.NotNull(profile);
        Assert.Equal("alpha", profile.Id);
    }

    [Fact]
    public void GetById_IsCaseInsensitive()
    {
        var service = CreateService(new Dictionary<string, BehaviorEntry>
        {
            ["alpha"] = new BehaviorEntry { Name = "Alpha", Description = "First", SystemPrompt = "Prompt A" }
        });

        var profile = service.GetById("ALPHA");

        Assert.NotNull(profile);
        Assert.Equal("alpha", profile.Id);
    }

    [Fact]
    public void GetById_WithUnknownId_ReturnsNull()
    {
        var service = CreateService(new Dictionary<string, BehaviorEntry>
        {
            ["alpha"] = new BehaviorEntry { Name = "Alpha", Description = "First", SystemPrompt = "Prompt A" }
        });

        var profile = service.GetById("does-not-exist");

        Assert.Null(profile);
    }

    [Fact]
    public void BehaviorProfile_MapsSystemPromptAndPreferredFields()
    {
        var service = CreateService(new Dictionary<string, BehaviorEntry>
        {
            ["my-behavior"] = new BehaviorEntry
            {
                Name = "My Behavior",
                Description = "Test",
                SystemPrompt = "You are a test assistant.",
                PreferredProviderName = "local",
                PreferredModelName = "llama3.2"
            }
        });

        var profile = service.GetById("my-behavior");

        Assert.NotNull(profile);
        Assert.Equal("You are a test assistant.", profile.SystemPrompt);
        Assert.Equal("local", profile.PreferredProviderName);
        Assert.Equal("llama3.2", profile.PreferredModelName);
    }

    [Fact]
    public void BehaviorProfile_NullPreferredFieldsPassThroughAsNull()
    {
        var service = CreateService(new Dictionary<string, BehaviorEntry>
        {
            ["no-prefs"] = new BehaviorEntry { Name = "No Prefs", Description = "Test", SystemPrompt = "Hi" }
        });

        var profile = service.GetById("no-prefs");

        Assert.NotNull(profile);
        Assert.Null(profile.PreferredProviderName);
        Assert.Null(profile.PreferredModelName);
    }

    [Fact]
    public void BehaviorProfile_MapsPluginsWithToolsAndConfigPreserved()
    {
        var service = CreateService(new Dictionary<string, BehaviorEntry>
        {
            ["with-plugins"] = new BehaviorEntry
            {
                Name = "With Plugins",
                Description = "Test",
                SystemPrompt = "Prompt",
                Plugins =
                [
                    new PluginEntry
                    {
                        Name = "qbittorrent",
                        Tools = ["ListTorrents", "SearchTorrents"],
                        Config = new Dictionary<string, string> { ["baseUrl"] = "http://localhost:8080", ["username"] = "admin" }
                    }
                ]
            }
        });

        var profile = service.GetById("with-plugins");

        Assert.NotNull(profile);
        Assert.Single(profile.Plugins);
        Assert.Equal("qbittorrent", profile.Plugins[0].Name);
        Assert.Equal(["ListTorrents", "SearchTorrents"], profile.Plugins[0].Tools);
        Assert.Equal("http://localhost:8080", profile.Plugins[0].Config["baseUrl"]);
        Assert.Equal("admin", profile.Plugins[0].Config["username"]);
    }

    [Fact]
    public void BehaviorProfile_EmptyToolsListPreservedAsEmpty()
    {
        var service = CreateService(new Dictionary<string, BehaviorEntry>
        {
            ["all-tools"] = new BehaviorEntry
            {
                Name = "All Tools",
                Description = "Test",
                SystemPrompt = "Prompt",
                Plugins = [new PluginEntry { Name = "qbittorrent", Tools = [] }]
            }
        });

        var profile = service.GetById("all-tools");

        Assert.NotNull(profile);
        Assert.Single(profile.Plugins);
        Assert.Empty(profile.Plugins[0].Tools);
    }

    [Fact]
    public void EmptyConfig_ProducesEmptyCatalog()
    {
        var service = CreateService(new Dictionary<string, BehaviorEntry>());

        var profiles = service.GetAll();

        Assert.Empty(profiles);
    }
}
