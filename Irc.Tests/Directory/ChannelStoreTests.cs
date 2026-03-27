using Irc.Contracts.Messages;
using Irc.Directory;

namespace Irc.Tests.Directory;

public class ChannelStoreTests
{
    [Test]
    public void ApplyChannelUpdate_PopulatesChannelsForServer()
    {
        var store = new ChannelStore();

        store.ApplyChannelUpdate(new ChannelUpdateMessage
        {
            ChatServerId = "acs-1",
            Channels =
            [
                new ChannelUpdateEntry { ChannelName = "%#Lobby", ChannelUid = "acs-1:100", MemberCount = 15 },
                new ChannelUpdateEntry { ChannelName = "%#Games", ChannelUid = "acs-1:101", MemberCount = 7 }
            ]
        });

        Assert.That(store.TotalChannelCount, Is.EqualTo(2));

        var channels = store.GetChannelsForServer("acs-1");
        Assert.That(channels, Has.Count.EqualTo(2));
        Assert.That(channels.Select(c => c.ChannelName), Is.EquivalentTo(new[] { "%#Lobby", "%#Games" }));

        var lobby = store.FindChannelByName("%#Lobby");
        Assert.That(lobby, Is.Not.Null);
        Assert.That(lobby!.ChannelUid, Is.EqualTo("acs-1:100"));
        Assert.That(lobby.MemberCount, Is.EqualTo(15));
        Assert.That(lobby.ChatServerId, Is.EqualTo("acs-1"));
    }

    [Test]
    public void ApplyChannelUpdate_FullReplacementForSameServer()
    {
        var store = new ChannelStore();

        // Initial update with two channels
        store.ApplyChannelUpdate(new ChannelUpdateMessage
        {
            ChatServerId = "acs-1",
            Channels =
            [
                new ChannelUpdateEntry { ChannelName = "%#Lobby", ChannelUid = "acs-1:100", MemberCount = 10 },
                new ChannelUpdateEntry { ChannelName = "%#OldRoom", ChannelUid = "acs-1:101", MemberCount = 3 }
            ]
        });

        Assert.That(store.TotalChannelCount, Is.EqualTo(2));

        // Second update: OldRoom gone, NewRoom added, Lobby updated
        store.ApplyChannelUpdate(new ChannelUpdateMessage
        {
            ChatServerId = "acs-1",
            Channels =
            [
                new ChannelUpdateEntry { ChannelName = "%#Lobby", ChannelUid = "acs-1:100", MemberCount = 20 },
                new ChannelUpdateEntry { ChannelName = "%#NewRoom", ChannelUid = "acs-1:102", MemberCount = 5 }
            ]
        });

        Assert.That(store.TotalChannelCount, Is.EqualTo(2));
        Assert.That(store.FindChannelByName("%#OldRoom"), Is.Null); // removed
        Assert.That(store.FindChannelByName("%#Lobby")!.MemberCount, Is.EqualTo(20)); // updated
        Assert.That(store.FindChannelByName("%#NewRoom"), Is.Not.Null); // added
    }

    [Test]
    public void ApplyChannelUpdate_EmptyChannels_RemovesAllForServer()
    {
        var store = new ChannelStore();

        store.ApplyChannelUpdate(new ChannelUpdateMessage
        {
            ChatServerId = "acs-1",
            Channels =
            [
                new ChannelUpdateEntry { ChannelName = "%#Lobby", ChannelUid = "acs-1:100", MemberCount = 10 }
            ]
        });

        Assert.That(store.TotalChannelCount, Is.EqualTo(1));

        // Empty snapshot — server has no channels anymore
        store.ApplyChannelUpdate(new ChannelUpdateMessage
        {
            ChatServerId = "acs-1",
            Channels = []
        });

        Assert.That(store.TotalChannelCount, Is.EqualTo(0));
        Assert.That(store.FindChannelByName("%#Lobby"), Is.Null);
    }

    [Test]
    public void ApplyChannelUpdate_MultipleServersIndependent()
    {
        var store = new ChannelStore();

        store.ApplyChannelUpdate(new ChannelUpdateMessage
        {
            ChatServerId = "acs-1",
            Channels =
            [
                new ChannelUpdateEntry { ChannelName = "%#Lobby", ChannelUid = "acs-1:100", MemberCount = 10 }
            ]
        });

        store.ApplyChannelUpdate(new ChannelUpdateMessage
        {
            ChatServerId = "acs-2",
            Channels =
            [
                new ChannelUpdateEntry { ChannelName = "%#Music", ChannelUid = "acs-2:200", MemberCount = 25 }
            ]
        });

        Assert.That(store.TotalChannelCount, Is.EqualTo(2));

        // Update for acs-1 does not affect acs-2
        store.ApplyChannelUpdate(new ChannelUpdateMessage
        {
            ChatServerId = "acs-1",
            Channels = []
        });

        Assert.That(store.TotalChannelCount, Is.EqualTo(1));
        Assert.That(store.FindChannelByName("%#Music"), Is.Not.Null);
        Assert.That(store.FindChannelByName("%#Music")!.ChatServerId, Is.EqualTo("acs-2"));
    }

    [Test]
    public void GetAllChannels_ReturnsFlatList()
    {
        var store = new ChannelStore();

        store.ApplyChannelUpdate(new ChannelUpdateMessage
        {
            ChatServerId = "acs-1",
            Channels =
            [
                new ChannelUpdateEntry { ChannelName = "%#Lobby", ChannelUid = "acs-1:100", MemberCount = 10 },
                new ChannelUpdateEntry { ChannelName = "%#Games", ChannelUid = "acs-1:101", MemberCount = 7 }
            ]
        });

        store.ApplyChannelUpdate(new ChannelUpdateMessage
        {
            ChatServerId = "acs-2",
            Channels =
            [
                new ChannelUpdateEntry { ChannelName = "%#Music", ChannelUid = "acs-2:200", MemberCount = 25 }
            ]
        });

        var all = store.GetAllChannels();
        Assert.That(all, Has.Count.EqualTo(3));
        Assert.That(all.Select(c => c.ChannelName), Is.EquivalentTo(new[] { "%#Lobby", "%#Games", "%#Music" }));
    }

    [Test]
    public void FindChannelByName_CaseInsensitive()
    {
        var store = new ChannelStore();

        store.ApplyChannelUpdate(new ChannelUpdateMessage
        {
            ChatServerId = "acs-1",
            Channels =
            [
                new ChannelUpdateEntry { ChannelName = "%#Lobby", ChannelUid = "acs-1:100", MemberCount = 10 }
            ]
        });

        // Channel name lookups should be case-insensitive
        Assert.That(store.FindChannelByName("%#LOBBY"), Is.Not.Null);
        Assert.That(store.FindChannelByName("%#lobby"), Is.Not.Null);
    }

    [Test]
    public void GetChannelsForServer_UnknownServer_ReturnsEmpty()
    {
        var store = new ChannelStore();
        var channels = store.GetChannelsForServer("nonexistent");
        Assert.That(channels, Is.Empty);
    }
}
