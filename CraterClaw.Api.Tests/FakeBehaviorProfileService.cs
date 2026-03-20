using CraterClaw.Core;

namespace CraterClaw.Api.Tests;

internal sealed class FakeBehaviorProfileService(IReadOnlyList<BehaviorProfile> profiles) : IBehaviorProfileService
{
    public IReadOnlyList<BehaviorProfile> GetAll() => profiles;

    public BehaviorProfile? GetById(string id) =>
        profiles.FirstOrDefault(p => p.Id == id);
}
