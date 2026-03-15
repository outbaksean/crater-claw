using Microsoft.Extensions.Options;

namespace CraterClaw.Core;

internal sealed class ProviderOptionsValidator : IValidateOptions<ProviderOptions>
{
    public ValidateOptionsResult Validate(string? name, ProviderOptions options)
    {
        if (options.Endpoints.Count == 0)
            return ValidateOptionsResult.Fail("Endpoints must contain at least one entry.");

        var failures = new List<string>();

        foreach (var (key, endpointOptions) in options.Endpoints)
        {
            if (!Uri.TryCreate(endpointOptions.BaseUrl, UriKind.Absolute, out _))
                failures.Add($"Endpoint '{key}' has an invalid BaseUrl: '{endpointOptions.BaseUrl}'.");
        }

        if (options.Active is not null && !options.Endpoints.ContainsKey(options.Active))
            failures.Add($"Active endpoint '{options.Active}' does not match any configured endpoint.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
