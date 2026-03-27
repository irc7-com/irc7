using Irc.Directory;
using DirectoryListx = Irc.Directory.Commands.Listx;

namespace Irc.Tests.Directory;

[TestFixture]
public class DirectoryListxTests
{
    private List<ChannelStoreEntry> _channels = null!;

    [SetUp]
    public void SetUp()
    {
        _channels =
        [
            new() { ChannelName = "%#Lobby", ChannelUid = "acs-1:100", MemberCount = 25, ChatServerId = "acs-1" },
            new() { ChannelName = "%#Games", ChannelUid = "acs-1:101", MemberCount = 8, ChatServerId = "acs-1" },
            new() { ChannelName = "%#Music", ChannelUid = "acs-2:200", MemberCount = 42, ChatServerId = "acs-2" },
            new() { ChannelName = "%#Help", ChannelUid = "acs-2:201", MemberCount = 3, ChatServerId = "acs-2" },
            new() { ChannelName = "%#Sports", ChannelUid = "acs-1:102", MemberCount = 15, ChatServerId = "acs-1" },
        ];
    }

    [Test]
    public void FilterChannels_NoFilter_ReturnsAllOrderedByName()
    {
        var (result, truncated) = DirectoryListx.FilterChannels(_channels, null);

        Assert.That(truncated, Is.False);
        Assert.That(result, Has.Count.EqualTo(5));
        // Should be alphabetically ordered by name
        Assert.That(result.Select(c => c.ChannelName).ToList(),
            Is.EqualTo(new[] { "%#Games", "%#Help", "%#Lobby", "%#Music", "%#Sports" }));
    }

    [Test]
    public void FilterChannels_MemberCountLessThan()
    {
        var (result, _) = DirectoryListx.FilterChannels(_channels, "<10");

        Assert.That(result.Select(c => c.ChannelName),
            Is.EquivalentTo(new[] { "%#Games", "%#Help" }));
    }

    [Test]
    public void FilterChannels_MemberCountGreaterThan()
    {
        var (result, _) = DirectoryListx.FilterChannels(_channels, ">20");

        Assert.That(result.Select(c => c.ChannelName),
            Is.EquivalentTo(new[] { "%#Lobby", "%#Music" }));
    }

    [Test]
    public void FilterChannels_NameMask_Wildcard()
    {
        var (result, _) = DirectoryListx.FilterChannels(_channels, "N=%#*o*");

        // Matches: %#Lobby, %#Sports (contains 'o')
        Assert.That(result.Select(c => c.ChannelName),
            Is.EquivalentTo(new[] { "%#Lobby", "%#Sports" }));
    }

    [Test]
    public void FilterChannels_NameMask_Exact()
    {
        var (result, _) = DirectoryListx.FilterChannels(_channels, "N=%#Music");

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].ChannelName, Is.EqualTo("%#Music"));
    }

    [Test]
    public void FilterChannels_QueryLimit_Truncates()
    {
        var (result, truncated) = DirectoryListx.FilterChannels(_channels, "3");

        Assert.That(truncated, Is.True);
        Assert.That(result, Has.Count.EqualTo(3));
        // First 3 alphabetically: %#Games, %#Help, %#Lobby
        Assert.That(result.Select(c => c.ChannelName).ToList(),
            Is.EqualTo(new[] { "%#Games", "%#Help", "%#Lobby" }));
    }

    [Test]
    public void FilterChannels_QueryLimit_NoTruncationWhenEnough()
    {
        var (result, truncated) = DirectoryListx.FilterChannels(_channels, "100");

        Assert.That(truncated, Is.False);
        Assert.That(result, Has.Count.EqualTo(5));
    }

    [Test]
    public void FilterChannels_CombinedFilters()
    {
        // Member count > 5 AND name matches *s*
        var (result, _) = DirectoryListx.FilterChannels(_channels, ">5,N=%#*s*");

        // >5: Games(8), Lobby(25), Music(42), Sports(15)
        // N=%#*s*: Games (Game"s"), Music (Mu"s"ic), Sports (Sport"s")
        Assert.That(result.Select(c => c.ChannelName),
            Is.EquivalentTo(new[] { "%#Games", "%#Music", "%#Sports" }));
    }

    [Test]
    public void FilterChannels_CombinedFiltersWithLimit()
    {
        var (result, truncated) = DirectoryListx.FilterChannels(_channels, ">5,1");

        Assert.That(truncated, Is.True);
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public void FilterChannels_UnsupportedFilters_SilentlyIgnored()
    {
        // R=1, T<5, S=*, L=en are all unsupported on ADS — should be ignored
        var (result, _) = DirectoryListx.FilterChannels(_channels, "R=1,T<5,S=*test*,L=en");

        // All channels should still be returned (unsupported filters have no effect)
        Assert.That(result, Has.Count.EqualTo(5));
    }

    [Test]
    public void FilterChannels_EmptyStore_ReturnsEmpty()
    {
        var (result, truncated) = DirectoryListx.FilterChannels(new List<ChannelStoreEntry>(), null);

        Assert.That(result, Is.Empty);
        Assert.That(truncated, Is.False);
    }

    [Test]
    public void FilterChannels_EmptyFilterString_ReturnsAll()
    {
        var (result, _) = DirectoryListx.FilterChannels(_channels, "");

        // Empty string produces no query terms from CSVToArray, so no filtering
        Assert.That(result, Has.Count.EqualTo(5));
    }
}
