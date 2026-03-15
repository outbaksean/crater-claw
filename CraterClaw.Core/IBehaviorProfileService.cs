namespace CraterClaw.Core;

public interface IBehaviorProfileService
{
    IReadOnlyList<BehaviorProfile> GetAll();
    BehaviorProfile? GetById(string id);
}
