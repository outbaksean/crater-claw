namespace CraterClaw.Core;

internal sealed class BehaviorProfileService : IBehaviorProfileService
{
    private static readonly IReadOnlyList<BehaviorProfile> Catalog =
    [
        new BehaviorProfile(
            "no-tools",
            "No Tools",
            "General-purpose conversation and reasoning with no external tools.",
            ["reasoning"],
            []),
        new BehaviorProfile(
            "qbittorrent-manager",
            "qBitTorrent Manager",
            "Querying and managing downloads using qBitTorrent.",
            ["reasoning"],
            ["qbittorrent"])
    ];

    public IReadOnlyList<BehaviorProfile> GetAll() => Catalog;

    public BehaviorProfile? GetById(string id) =>
        Catalog.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
}
