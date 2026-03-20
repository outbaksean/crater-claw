using Microsoft.Extensions.Options;

namespace CraterClaw.Core;

internal sealed class QBitTorrentOptionsValidator : IValidateOptions<QBitTorrentOptions>
{
    public ValidateOptionsResult Validate(string? name, QBitTorrentOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            return ValidateOptionsResult.Success;

        var failures = new List<string>();

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            failures.Add($"qbittorrent:baseUrl '{options.BaseUrl}' is not a valid URI.");

        if (string.IsNullOrWhiteSpace(options.Username))
            failures.Add("qbittorrent:username is required when baseUrl is set.");

        if (string.IsNullOrWhiteSpace(options.Password))
            failures.Add("qbittorrent:password is required when baseUrl is set.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
