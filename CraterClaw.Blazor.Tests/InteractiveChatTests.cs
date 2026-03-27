using Bunit;
using CraterClaw.Blazor.Api;
using CraterClaw.Blazor.Components;
using Microsoft.Extensions.DependencyInjection;

namespace CraterClaw.Blazor.Tests;

public class InteractiveChatTests : TestContext
{
    private CraterClawClient CreateClient(ExecutionResponse response)
    {
        var http = new HttpClient(new TestHttpHandler(_ => TestHttpHandler.Json(response)))
        {
            BaseAddress = new Uri("http://test/")
        };
        return new CraterClawClient(http);
    }

    [Fact]
    public void RendersEmptyConversationInitially()
    {
        Services.AddSingleton(CreateClient(new ExecutionResponse("", "m", "Stop")));
        var cut = Render<InteractiveChat>(p => p
            .Add(x => x.ProviderName, "local")
            .Add(x => x.ModelName, "llama3"));

        Assert.DoesNotContain("class=\"message", cut.Markup);
    }

    [Fact]
    public async Task SubmittingMessageAppendsUserThenAssistantTurn()
    {
        Services.AddSingleton(CreateClient(new ExecutionResponse("World", "llama3", "Stop")));
        var cut = Render<InteractiveChat>(p => p
            .Add(x => x.ProviderName, "local")
            .Add(x => x.ModelName, "llama3"));

        cut.Find("textarea").Input("Hello");
        await cut.Find("button").ClickAsync(new());

        var messages = cut.FindAll(".message");
        Assert.Equal(2, messages.Count);
        Assert.Contains("Hello", messages[0].TextContent);
        Assert.Contains("World", messages[1].TextContent);
    }

    [Fact]
    public void SubmitButtonDisabledWhenInputIsEmpty()
    {
        Services.AddSingleton(CreateClient(new ExecutionResponse("", "m", "Stop")));
        var cut = Render<InteractiveChat>(p => p
            .Add(x => x.ProviderName, "local")
            .Add(x => x.ModelName, "llama3"));

        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
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
        var cut = Render<InteractiveChat>(p => p
            .Add(x => x.ProviderName, "local")
            .Add(x => x.ModelName, "llama3"));

        cut.Find("textarea").Input("Hello");
        await cut.Find("button").ClickAsync(new());

        Assert.Contains("error-msg", cut.Markup);
    }
}
