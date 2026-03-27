using Irc.ChannelMaster.Broadcast;
using Irc.ChannelMaster.Models;
using Irc.ChannelMaster.State;
using Irc.Contracts.Messages;

namespace Irc.ChannelMaster.Tests.Broadcast;

public class BroadcastProcessTests
{
    [Test]
    public async Task RunOnce_ReturnsOnlyAssignedChatServersForThisWorker()
    {
        var store = new InMemoryChannelMasterStore();
        var process = new BroadcastProcess(store, "worker-A");

        await store.SetChatServerAssignmentAsync("chat-1", "worker-A");
        await store.SetChatServerAssignmentAsync("chat-2", "worker-B");
        await store.SetChatServerAssignmentAsync("chat-3", "worker-A");

        var snapshot = await process.RunOnceAsync(5);

        Assert.That(snapshot.WorkerId, Is.EqualTo("worker-A"));
        Assert.That(snapshot.ReportedLoad, Is.EqualTo(5));
        Assert.That(snapshot.ChatServerIds, Is.EqualTo(new[] { "chat-1", "chat-3" }));
    }

    [Test]
    public async Task RunOnce_HeartbeatsWorker()
    {
        var store = new InMemoryChannelMasterStore();
        var process = new BroadcastProcess(store, "worker-A");

        var snapshot = await process.RunOnceAsync(3);
        var workers = await store.GetActiveBroadcastWorkersAsync();

        Assert.That(snapshot.ChatServerIds, Is.Empty);
        Assert.That(workers.Any(w => w.WorkerId == "worker-A"), Is.True);
    }

    // ── Phase 3B: CHAT-UPDATE Tests ─────────────────────────────────────

    [Test]
    public async Task HandleChatUpdate_UpdatesMemberCountOnExistingChannel()
    {
        var store = new InMemoryChannelMasterStore();
        var process = new BroadcastProcess(store, "worker-A");

        // Pre-create a channel
        await store.TryClaimChannelAsync("%#Lobby", "acs-1:100", "acs-1", DateTime.UtcNow);
        var recordBefore = await store.GetChannelRecordAsync("%#Lobby");
        Assert.That(recordBefore!.MemberCount, Is.EqualTo(0));

        // Receive CHAT-UPDATE with 15 members
        await process.HandleChatUpdateAsync(new ChatUpdateMessage
        {
            ChatServerId = "acs-1",
            Entries = [new ChatUpdateEntry { ChannelName = "%#Lobby", MemberCount = 15 }]
        });

        var recordAfter = await store.GetChannelRecordAsync("%#Lobby");
        Assert.That(recordAfter!.MemberCount, Is.EqualTo(15));
    }

    [Test]
    public async Task HandleChatUpdate_ZeroMemberCount_UncliamsChannel()
    {
        var store = new InMemoryChannelMasterStore();
        var process = new BroadcastProcess(store, "worker-A");

        await store.TryClaimChannelAsync("%#Temp", "acs-1:200", "acs-1", DateTime.UtcNow);

        // Receive CHAT-UPDATE with zero members — channel closed
        await process.HandleChatUpdateAsync(new ChatUpdateMessage
        {
            ChatServerId = "acs-1",
            Entries = [new ChatUpdateEntry { ChannelName = "%#Temp", MemberCount = 0 }]
        });

        var record = await store.GetChannelRecordAsync("%#Temp");
        Assert.That(record, Is.Null);
    }

    [Test]
    public async Task HandleChatUpdate_IgnoresUnknownChannel()
    {
        var store = new InMemoryChannelMasterStore();
        var process = new BroadcastProcess(store, "worker-A");

        // No channel claimed — should not throw
        await process.HandleChatUpdateAsync(new ChatUpdateMessage
        {
            ChatServerId = "acs-1",
            Entries = [new ChatUpdateEntry { ChannelName = "%#Unknown", MemberCount = 5 }]
        });

        var record = await store.GetChannelRecordAsync("%#Unknown");
        Assert.That(record, Is.Null);
    }

    [Test]
    public async Task HandleChatUpdate_MultipleEntries_UpdatesAll()
    {
        var store = new InMemoryChannelMasterStore();
        var process = new BroadcastProcess(store, "worker-A");

        await store.TryClaimChannelAsync("%#Alpha", "acs-1:300", "acs-1", DateTime.UtcNow);
        await store.TryClaimChannelAsync("%#Beta", "acs-1:301", "acs-1", DateTime.UtcNow);
        await store.TryClaimChannelAsync("%#Gamma", "acs-1:302", "acs-1", DateTime.UtcNow);

        await process.HandleChatUpdateAsync(new ChatUpdateMessage
        {
            ChatServerId = "acs-1",
            Entries =
            [
                new ChatUpdateEntry { ChannelName = "%#Alpha", MemberCount = 10 },
                new ChatUpdateEntry { ChannelName = "%#Beta", MemberCount = 0 },  // closed
                new ChatUpdateEntry { ChannelName = "%#Gamma", MemberCount = 3 }
            ]
        });

        var alpha = await store.GetChannelRecordAsync("%#Alpha");
        Assert.That(alpha!.MemberCount, Is.EqualTo(10));

        var beta = await store.GetChannelRecordAsync("%#Beta");
        Assert.That(beta, Is.Null); // unclaimed due to zero count

        var gamma = await store.GetChannelRecordAsync("%#Gamma");
        Assert.That(gamma!.MemberCount, Is.EqualTo(3));
    }

    [Test]
    public async Task HandleChatUpdate_EmptyEntries_DoesNothing()
    {
        var store = new InMemoryChannelMasterStore();
        var process = new BroadcastProcess(store, "worker-A");

        // Should not throw on empty entries
        await process.HandleChatUpdateAsync(new ChatUpdateMessage
        {
            ChatServerId = "acs-1",
            Entries = []
        });
    }

    // ── Phase 3C: CHANNEL-UPDATE Build Tests ────────────────────────────

    [Test]
    public async Task BuildChannelUpdates_ReturnsOneMessagePerAssignedChatServer()
    {
        var store = new InMemoryChannelMasterStore();
        var process = new BroadcastProcess(store, "worker-A");

        // Assign two chat servers to worker-A
        await store.SetChatServerAssignmentAsync("acs-1", "worker-A");
        await store.SetChatServerAssignmentAsync("acs-2", "worker-A");
        await store.SetChatServerAssignmentAsync("acs-3", "worker-B"); // different worker

        // Create channels on acs-1 and acs-2
        await store.TryClaimChannelAsync("%#Lobby", "acs-1:100", "acs-1", DateTime.UtcNow);
        await store.TryClaimChannelAsync("%#Games", "acs-1:101", "acs-1", DateTime.UtcNow);
        await store.TryClaimChannelAsync("%#Music", "acs-2:200", "acs-2", DateTime.UtcNow);

        // Set some member counts
        await store.UpdateChannelMemberCountAsync("%#Lobby", 15);
        await store.UpdateChannelMemberCountAsync("%#Games", 7);
        await store.UpdateChannelMemberCountAsync("%#Music", 42);

        var messages = await process.BuildChannelUpdatesAsync();

        Assert.That(messages, Has.Count.EqualTo(2));

        var msg1 = messages.First(m => m.ChatServerId == "acs-1");
        Assert.That(msg1.Channels, Has.Length.EqualTo(2));
        Assert.That(msg1.Channels.Select(c => c.ChannelName), Is.EquivalentTo(new[] { "%#Games", "%#Lobby" }));
        var lobby = msg1.Channels.First(c => c.ChannelName == "%#Lobby");
        Assert.That(lobby.ChannelUid, Is.EqualTo("acs-1:100"));
        Assert.That(lobby.MemberCount, Is.EqualTo(15));

        var msg2 = messages.First(m => m.ChatServerId == "acs-2");
        Assert.That(msg2.Channels, Has.Length.EqualTo(1));
        Assert.That(msg2.Channels[0].ChannelName, Is.EqualTo("%#Music"));
        Assert.That(msg2.Channels[0].MemberCount, Is.EqualTo(42));
    }

    [Test]
    public async Task BuildChannelUpdates_EmptySnapshotForServerWithNoChannels()
    {
        var store = new InMemoryChannelMasterStore();
        var process = new BroadcastProcess(store, "worker-A");

        // Assign acs-1 to worker-A but give it no channels
        await store.SetChatServerAssignmentAsync("acs-1", "worker-A");

        var messages = await process.BuildChannelUpdatesAsync();

        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].ChatServerId, Is.EqualTo("acs-1"));
        Assert.That(messages[0].Channels, Is.Empty);
    }

    [Test]
    public async Task BuildChannelUpdates_NoAssignedServers_ReturnsEmpty()
    {
        var store = new InMemoryChannelMasterStore();
        var process = new BroadcastProcess(store, "worker-A");

        // No assignments for worker-A
        await store.SetChatServerAssignmentAsync("acs-1", "worker-B");

        var messages = await process.BuildChannelUpdatesAsync();

        Assert.That(messages, Is.Empty);
    }

    [Test]
    public async Task BuildChannelUpdates_ExcludesChannelsFromUnassignedServers()
    {
        var store = new InMemoryChannelMasterStore();
        var process = new BroadcastProcess(store, "worker-A");

        await store.SetChatServerAssignmentAsync("acs-1", "worker-A");

        // Channel on acs-1 (assigned to worker-A) and acs-2 (not assigned)
        await store.TryClaimChannelAsync("%#Lobby", "acs-1:100", "acs-1", DateTime.UtcNow);
        await store.TryClaimChannelAsync("%#Other", "acs-2:200", "acs-2", DateTime.UtcNow);

        var messages = await process.BuildChannelUpdatesAsync();

        Assert.That(messages, Has.Count.EqualTo(1));
        Assert.That(messages[0].ChatServerId, Is.EqualTo("acs-1"));
        Assert.That(messages[0].Channels, Has.Length.EqualTo(1));
        Assert.That(messages[0].Channels[0].ChannelName, Is.EqualTo("%#Lobby"));
    }
}
