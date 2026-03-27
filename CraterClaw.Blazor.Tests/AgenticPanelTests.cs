using Bunit;
using CraterClaw.Blazor.Api;
using CraterClaw.Blazor.Components;
using Microsoft.Extensions.DependencyInjection;

namespace CraterClaw.Blazor.Tests;

public class AgenticPanelTests : TestContext
{
    private CraterClawClient CreateClient(AgenticResponse response)
    {
        var http = new HttpClient(new TestHttpHandler(_ => TestHttpHandler.Json(response)))
        {
            BaseAddress = new Uri("http://test/")
        };
        return new CraterClawClient(http);
    }

    private IRenderedComponent<AgenticPanel> RenderPanel(AgenticResponse response)
    {
        Services.AddSingleton(CreateClient(response));
        return Render<AgenticPanel>(p => p
            .Add(x => x.ProviderName, "local")
            .Add(x => x.ModelName, "llama3")
            .Add(x => x.ProfileId, "no-tools"));
    }

    [Fact]
    public void NoResultRenderedInitially()
    {
        var cut = RenderPanel(new AgenticResponse("", "Completed", []));
        Assert.DoesNotContain("agentic-result", cut.Markup);
    }

    [Fact]
    public async Task SubmittingTaskDisplaysResponseContent()
    {
        var cut = RenderPanel(new AgenticResponse("Task done.", "Completed", []));

        cut.Find("textarea").Input("Do the thing");
        await cut.Find("button").ClickAsync(new());

        Assert.Contains("Task done.", cut.Markup);
    }

    [Fact]
    public async Task DisplaysFinishReason()
    {
        var cut = RenderPanel(new AgenticResponse("Result", "Completed", []));

        cut.Find("textarea").Input("Do the thing");
        await cut.Find("button").ClickAsync(new());

        Assert.Contains("Completed", cut.Markup);
    }

    [Fact]
    public async Task DisplaysToolsInvokedList()
    {
        var cut = RenderPanel(
            new AgenticResponse("Done", "Completed", ["ListTorrents", "AddTorrentByUrl"]));

        cut.Find("textarea").Input("Do the thing");
        await cut.Find("button").ClickAsync(new());

        Assert.Contains("ListTorrents", cut.Markup);
        Assert.Contains("AddTorrentByUrl", cut.Markup);
    }

    [Fact]
    public void SubmitButtonDisabledWhenPromptIsEmpty()
    {
        var cut = RenderPanel(new AgenticResponse("", "Completed", []));
        Assert.True(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public async Task ShowsErrorOnFailure()
    {
        var http = new HttpClient(new TestHttpHandler(_ =>
            new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)))
        {
            BaseAddress = new Uri("http://test/")
        };
        Services.AddSingleton(new CraterClawClient(http));
        var cut = Render<AgenticPanel>(p => p
            .Add(x => x.ProviderName, "local")
            .Add(x => x.ModelName, "llama3")
            .Add(x => x.ProfileId, "no-tools"));

        cut.Find("textarea").Input("Do the thing");
        await cut.Find("button").ClickAsync(new());

        Assert.Contains("error-msg", cut.Markup);
    }
}
