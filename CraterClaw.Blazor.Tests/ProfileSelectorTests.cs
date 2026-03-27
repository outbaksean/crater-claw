using Bunit;
using CraterClaw.Blazor.Api;
using CraterClaw.Blazor.Components;

namespace CraterClaw.Blazor.Tests;

public class ProfileSelectorTests : TestContext
{
    private static readonly List<BehaviorProfile> TwoProfiles =
    [
        new("no-tools", "No Tools", "Plain chat without plugins", "", null, null, []),
        new("qbt-home", "qBitTorrent Home", "Manage home torrents", "", null, null, []),
    ];

    [Fact]
    public void RendersNumberedProfileList()
    {
        var cut = Render<ProfileSelector>(p => p
            .Add(x => x.Profiles, TwoProfiles));

        var items = cut.FindAll("ol li");
        Assert.Equal(2, items.Count);
        Assert.Contains("No Tools", items[0].TextContent);
        Assert.Contains("qBitTorrent Home", items[1].TextContent);
    }

    [Fact]
    public void RendersProfileDescriptions()
    {
        var cut = Render<ProfileSelector>(p => p
            .Add(x => x.Profiles, TwoProfiles));

        Assert.Contains("Plain chat without plugins", cut.Markup);
    }

    [Fact]
    public void ShowsNoProfilesMessageWhenListIsEmpty()
    {
        var cut = Render<ProfileSelector>(p => p
            .Add(x => x.Profiles, []));

        Assert.Contains("No profiles configured", cut.Markup);
    }

    [Fact]
    public void SelectingProfileRaisesCallback()
    {
        BehaviorProfile? selected = null;
        var cut = Render<ProfileSelector>(p => p
            .Add(x => x.Profiles, TwoProfiles)
            .Add(x => x.OnProfileSelected, p => selected = p));

        cut.FindAll("button")[1].Click();

        Assert.NotNull(selected);
        Assert.Equal("qbt-home", selected.Id);
    }

    [Fact]
    public void RendersWarningWhenProvided()
    {
        var cut = Render<ProfileSelector>(p => p
            .Add(x => x.Profiles, TwoProfiles)
            .Add(x => x.Warning, "Preferred provider 'remote' is not available."));

        Assert.Contains("Preferred provider", cut.Markup);
    }

    [Fact]
    public void NoWarningRenderedWhenNull()
    {
        var cut = Render<ProfileSelector>(p => p
            .Add(x => x.Profiles, TwoProfiles)
            .Add(x => x.Warning, (string?)null));

        Assert.DoesNotContain("warning-msg", cut.Markup);
    }
}
