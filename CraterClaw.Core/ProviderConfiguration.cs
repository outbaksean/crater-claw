namespace CraterClaw.Core;

public sealed record ProviderConfiguration(IReadOnlyList<ProviderEndpoint> Endpoints, string? ActiveProviderName)
{
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (Endpoints.Count == 0)
        {
            errors.Add("At least one provider endpoint must be configured.");
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var endpoint in Endpoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Name))
            {
                errors.Add("Provider endpoint name is required.");
            }
            else if (!names.Add(endpoint.Name))
            {
                errors.Add($"Duplicate provider endpoint name '{endpoint.Name}'.");
            }

            if (string.IsNullOrWhiteSpace(endpoint.BaseUrl) ||
                !Uri.TryCreate(endpoint.BaseUrl, UriKind.Absolute, out _))
            {
                errors.Add($"Provider endpoint '{endpoint.Name}' has an invalid BaseUrl.");
            }
        }

        if (!string.IsNullOrWhiteSpace(ActiveProviderName) &&
            !Endpoints.Any(e => string.Equals(e.Name, ActiveProviderName, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add($"Active provider '{ActiveProviderName}' is not in configured endpoints.");
        }

        return errors;
    }

    public void ValidateOrThrow()
    {
        var errors = Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(" ", errors));
        }
    }
}
