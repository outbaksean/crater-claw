using CraterClaw.Api.Models;
using CraterClaw.Core;

namespace CraterClaw.Api.Endpoints;

internal static class ProfilesEndpoints
{
    public static void MapProfileEndpoints(this WebApplication app)
    {
        app.MapGet("/api/profiles", (IBehaviorProfileService profileService) =>
        {
            var items = profileService.GetAll().Select(p => new BehaviorProfileApiItem(
                p.Id, p.Name, p.Description, p.SystemPrompt,
                p.PreferredProviderName, p.PreferredModelName,
                p.Plugins.Select(b => new PluginBindingApiItem(b.Name, b.Tools)).ToList())).ToList();
            return Results.Ok(items);
        });
    }
}
