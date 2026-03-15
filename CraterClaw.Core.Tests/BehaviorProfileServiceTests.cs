namespace CraterClaw.Core.Tests;

public sealed class BehaviorProfileServiceTests
{
    private readonly IBehaviorProfileService _service = new BehaviorProfileService();

    [Fact]
    public void GetAll_ReturnsTwoProfiles()
    {
        var profiles = _service.GetAll();

        Assert.Equal(2, profiles.Count);
    }

    [Fact]
    public void GetAll_AllIdentifiersAreUnique()
    {
        var profiles = _service.GetAll();
        var ids = profiles.Select(p => p.Id.ToLowerInvariant()).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Theory]
    [InlineData("no-tools")]
    [InlineData("qbittorrent-manager")]
    public void GetAll_ContainsExpectedIdentifier(string id)
    {
        var profiles = _service.GetAll();

        Assert.Contains(profiles, p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetById_WithValidId_ReturnsMatchingProfile()
    {
        var profile = _service.GetById("no-tools");

        Assert.NotNull(profile);
        Assert.Equal("no-tools", profile.Id);
    }

    [Fact]
    public void GetById_IsCaseInsensitive()
    {
        var profile = _service.GetById("NO-TOOLS");

        Assert.NotNull(profile);
        Assert.Equal("no-tools", profile.Id);
    }

    [Fact]
    public void GetById_WithUnknownId_ReturnsNull()
    {
        var profile = _service.GetById("does-not-exist");

        Assert.Null(profile);
    }

    [Fact]
    public void NoTools_HasEmptyAllowedMcpServerNames()
    {
        var profile = _service.GetById("no-tools");

        Assert.NotNull(profile);
        Assert.Empty(profile.AllowedMcpServerNames);
    }

    [Fact]
    public void QbittorrentManager_AllowsQbittorrentServer()
    {
        var profile = _service.GetById("qbittorrent-manager");

        Assert.NotNull(profile);
        Assert.Contains("qbittorrent", profile.AllowedMcpServerNames);
    }
}
