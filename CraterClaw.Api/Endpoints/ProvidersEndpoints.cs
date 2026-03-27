using CraterClaw.Api.Models;
using CraterClaw.Api.Services;
using CraterClaw.Core;

namespace CraterClaw.Api.Endpoints;

internal static class ProvidersEndpoints
{
    public static void MapProviderEndpoints(this WebApplication app)
    {
        app.MapGet("/api/providers", (IProviderResolver resolver) =>
        {
            var endpoints = resolver.GetAll()
                .Select(e => new ProviderEndpointResponse(e.Name, e.BaseUrl))
                .ToList();
            return Results.Ok(endpoints);
        });

        app.MapGet("/api/providers/{name}/status", async (
            string name,
            IProviderResolver resolver,
            IProviderStatusService statusService,
            CancellationToken cancellationToken) =>
        {
            var endpoint = resolver.Resolve(name);
            if (endpoint is null)
                return Results.NotFound();

            var status = await statusService.CheckStatusAsync(endpoint, cancellationToken);
            return Results.Ok(new ProviderStatusResponse(status.IsReachable, status.ErrorMessage));
        });

        app.MapGet("/api/providers/{name}/models", async (
            string name,
            IProviderResolver resolver,
            IModelListingService modelListingService,
            CancellationToken cancellationToken) =>
        {
            var endpoint = resolver.Resolve(name);
            if (endpoint is null)
                return Results.NotFound();

            var models = await modelListingService.ListModelsAsync(endpoint, cancellationToken);
            return Results.Ok(models.Select(m => new ModelApiItem(m.Name, m.SizeBytes, m.ModifiedAt)).ToList());
        });

        app.MapPost("/api/providers/{name}/execute", async (
            string name,
            ExecutionApiRequest request,
            IProviderResolver resolver,
            IModelExecutionService executionService,
            CancellationToken cancellationToken) =>
        {
            var endpoint = resolver.Resolve(name);
            if (endpoint is null)
                return Results.NotFound();

            var messages = request.Messages
                .Select(m => new ConversationMessage(m.Role, m.Content))
                .ToList();
            var executionRequest = new ExecutionRequest(request.ModelName, messages, request.Temperature, request.MaxTokens);
            var result = await executionService.ExecuteAsync(endpoint, executionRequest, cancellationToken);
            return Results.Ok(new ExecutionApiResponse(result.Content, result.ModelName, result.FinishReason));
        });

        app.MapPost("/api/providers/{name}/agentic", async (
            string name,
            AgenticApiRequest request,
            IProviderResolver resolver,
            IBehaviorProfileService profileService,
            IAgenticExecutionService agenticService,
            IPluginRegistry pluginRegistry,
            CancellationToken cancellationToken) =>
        {
            var endpoint = resolver.Resolve(name);
            if (endpoint is null)
                return Results.NotFound();

            var profile = profileService.GetById(request.ProfileId);
            if (profile is null)
                return Results.BadRequest($"Profile '{request.ProfileId}' not found.");

            var plugins = pluginRegistry.Resolve(profile.Plugins);

            var agenticRequest = new AgenticRequest(
                request.ModelName,
                request.Prompt,
                plugins,
                request.MaxIterations ?? 10,
                SystemPrompt: profile.SystemPrompt);

            var result = await agenticService.ExecuteAsync(endpoint, agenticRequest, cancellationToken);
            return Results.Ok(new AgenticApiResponse(result.Content, result.FinishReason, result.ToolsInvoked));
        });
    }
}
