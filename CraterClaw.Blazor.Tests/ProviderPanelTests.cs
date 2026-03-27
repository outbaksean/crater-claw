using Bunit;
using CraterClaw.Blazor.Api;
using CraterClaw.Blazor.Components;

namespace CraterClaw.Blazor.Tests;

public class ProviderPanelTests : TestContext
{
    private static readonly List<ProviderEndpoint> TwoProviders =
    [
        new("local", "http://localhost:11434"),
        new("lan", "http://192.168.1.50:11434"),
    ];

    [Fact]
    public void RendersNumberedListOfProviders()
    {
        var cut = Render<ProviderPanel>(p => p
            .Add(x => x.Providers, TwoProviders));

        var items = cut.FindAll("ol li");
        Assert.Equal(2, items.Count);
        Assert.Contains("local", items[0].TextContent);
        Assert.Contains("lan", items[1].TextContent);
    }

    [Fact]
    public void ShowsNoProvidersMessageWhenListIsEmpty()
    {
        var cut = Render<ProviderPanel>(p => p
            .Add(x => x.Providers, []));

        Assert.Contains("No providers configured", cut.Markup);
    }

    [Fact]
    public void SelectingProviderRaisesCallback()
    {
        ProviderEndpoint? selected = null;
        var cut = Render<ProviderPanel>(p => p
            .Add(x => x.Providers, TwoProviders)
            .Add(x => x.OnProviderSelected, ep => selected = ep));

        cut.FindAll("button")[1].Click();

        Assert.NotNull(selected);
        Assert.Equal("lan", selected.Name);
    }

    [Fact]
    public void ShowsReachableStatusPill()
    {
        var cut = Render<ProviderPanel>(p => p
            .Add(x => x.Providers, TwoProviders)
            .Add(x => x.Selected, TwoProviders[0])
            .Add(x => x.Status, new ProviderStatus(true, null))
            .Add(x => x.StatusLoading, false));

        Assert.Contains("reachable", cut.Markup);
    }

    [Fact]
    public void ShowsUnreachableStatusWithErrorMessage()
    {
        var cut = Render<ProviderPanel>(p => p
            .Add(x => x.Providers, TwoProviders)
            .Add(x => x.Selected, TwoProviders[0])
            .Add(x => x.Status, new ProviderStatus(false, "Connection refused"))
            .Add(x => x.StatusLoading, false));

        Assert.Contains("unreachable", cut.Markup);
        Assert.Contains("Connection refused", cut.Markup);
    }

    [Fact]
    public void ShowsLoadingPillWhenStatusLoading()
    {
        var cut = Render<ProviderPanel>(p => p
            .Add(x => x.Providers, TwoProviders)
            .Add(x => x.Selected, TwoProviders[0])
            .Add(x => x.StatusLoading, true));

        Assert.Contains("checking", cut.Markup);
    }

    [Fact]
    public void SelectedProviderButtonHasSelectedClass()
    {
        var cut = Render<ProviderPanel>(p => p
            .Add(x => x.Providers, TwoProviders)
            .Add(x => x.Selected, TwoProviders[0]));

        var buttons = cut.FindAll("button");
        Assert.Contains("selected", buttons[0].GetAttribute("class") ?? "");
        Assert.DoesNotContain("selected", buttons[1].GetAttribute("class") ?? "");
    }
}
