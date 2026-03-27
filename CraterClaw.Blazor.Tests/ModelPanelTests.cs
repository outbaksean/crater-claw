using Bunit;
using CraterClaw.Blazor.Api;
using CraterClaw.Blazor.Components;

namespace CraterClaw.Blazor.Tests;

public class ModelPanelTests : TestContext
{
    private static readonly List<ModelItem> TwoModels =
    [
        new("llama3", 4000000000, "2024-01-01T00:00:00Z"),
        new("mistral", 3800000000, "2024-01-01T00:00:00Z"),
    ];

    [Fact]
    public void RendersNumberedListOfModels()
    {
        var cut = Render<ModelPanel>(p => p
            .Add(x => x.Models, TwoModels));

        var items = cut.FindAll("ol li");
        Assert.Equal(2, items.Count);
        Assert.Contains("llama3", items[0].TextContent);
        Assert.Contains("mistral", items[1].TextContent);
    }

    [Fact]
    public void HiddenWhenModelsListIsEmpty()
    {
        var cut = Render<ModelPanel>(p => p
            .Add(x => x.Models, []));

        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void ShowsLoadingMessageWhenLoading()
    {
        var cut = Render<ModelPanel>(p => p
            .Add(x => x.Models, [])
            .Add(x => x.Loading, true));

        Assert.Contains("Loading models", cut.Markup);
    }

    [Fact]
    public void SelectingModelRaisesCallback()
    {
        ModelItem? selected = null;
        var cut = Render<ModelPanel>(p => p
            .Add(x => x.Models, TwoModels)
            .Add(x => x.OnModelSelected, m => selected = m));

        cut.FindAll("button")[0].Click();

        Assert.NotNull(selected);
        Assert.Equal("llama3", selected.Name);
    }

    [Fact]
    public void SelectedModelButtonHasSelectedClass()
    {
        var cut = Render<ModelPanel>(p => p
            .Add(x => x.Models, TwoModels)
            .Add(x => x.Selected, TwoModels[1]));

        var buttons = cut.FindAll("button");
        Assert.DoesNotContain("selected", buttons[0].GetAttribute("class") ?? "");
        Assert.Contains("selected", buttons[1].GetAttribute("class") ?? "");
    }
}
