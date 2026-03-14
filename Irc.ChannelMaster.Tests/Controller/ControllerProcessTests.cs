using Irc.ChannelMaster.Controller;
using Irc.ChannelMaster.Models;
using Irc.ChannelMaster.State;

namespace Irc.ChannelMaster.Tests.Controller;

public class ControllerProcessTests
{
    [Test]
    public async Task RunOnce_AssignsChatServersAcrossBroadcastWorkers()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatBroadcastWorkerAsync("worker-1", 0, TimeSpan.FromMinutes(1));
        await store.HeartbeatBroadcastWorkerAsync("worker-2", 0, TimeSpan.FromMinutes(1));

        await store.HeartbeatChatServerAsync("chat-1", 10, TimeSpan.FromMinutes(1));
        await store.HeartbeatChatServerAsync("chat-2", 8, TimeSpan.FromMinutes(1));
        await store.HeartbeatChatServerAsync("chat-3", 4, TimeSpan.FromMinutes(1));

        var ranAsLeader = await controller.RunOnceAsync();
        var assignments = await store.GetChatServerAssignmentsAsync();

        Assert.That(ranAsLeader, Is.True);
        Assert.That(assignments.Count, Is.EqualTo(3));
        Assert.That(assignments.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task RunOnce_DoesNotActAsLeader_WhenControllerLeaseIsHeldByAnotherInstance()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.TryAcquireControllerLeaseAsync("controller-B", TimeSpan.FromMinutes(1));

        var ranAsLeader = await controller.RunOnceAsync();

        Assert.That(ranAsLeader, Is.False);
        Assert.That(controller.IsLeader, Is.False);
    }

    [Test]
    public async Task CreateChannelAsync_AssignsLeastLoadedChatServer_AndClaimsCaseInsensitiveName()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatChatServerAsync("acs-2", 10, TimeSpan.FromMinutes(1));
        await store.HeartbeatChatServerAsync("acs-1", 2, TimeSpan.FromMinutes(1));

        _ = await controller.RunOnceAsync();

        var before = DateTime.UtcNow;
        var first = await controller.CreateChannelAsync("%#General");
        var after = DateTime.UtcNow;
        var second = await controller.CreateChannelAsync("%#general");
        var record = await controller.GetChannelRecordAsync("%#GENERAL");

        Assert.That(first.Status, Is.EqualTo(CreateChannelStatus.Success));
        Assert.That(first.ServerId, Is.EqualTo("acs-1"));
        Assert.That(first.ChannelUid, Is.Not.Null);
        Assert.That(first.ChannelUid, Does.StartWith("acs-1:"));
        Assert.That(second.Status, Is.EqualTo(CreateChannelStatus.NameConflict));
        Assert.That(record, Is.Not.Null);
        Assert.That(record!.OwnerServerId, Is.EqualTo("acs-1"));
        Assert.That(record.ChannelUid, Is.EqualTo(first.ChannelUid));
        Assert.That(record.ChannelName, Is.EqualTo("%#General"));
        Assert.That(record.CreatedUtc, Is.InRange(before, after));
    }

    [Test]
    public async Task RunOnce_ElectsMaximumIdAsLeader_WhenNoLeaderExists()
    {
        var store = new InMemoryChannelMasterStore();
        var low = new ControllerProcess(store, "cm-01") { LeaderPollRepeats = 1, LeaderPollInterval = TimeSpan.Zero };
        var high = new ControllerProcess(store, "cm-99") { LeaderPollRepeats = 1, LeaderPollInterval = TimeSpan.Zero };

        await store.HeartbeatChannelMasterAsync("cm-01", TimeSpan.FromMinutes(1));
        await store.HeartbeatChannelMasterAsync("cm-99", TimeSpan.FromMinutes(1));

        var lowLeader = await low.RunOnceAsync();
        var highLeader = await high.RunOnceAsync();
        var clusterLeader = await store.GetCurrentLeaderAsync();

        Assert.That(lowLeader, Is.False);
        Assert.That(highLeader, Is.True);
        Assert.That(clusterLeader, Is.EqualTo("cm-99"));
    }

    [Test]
    public async Task HandleCommandAsync_Create_ReturnsBusy_WhenNoChatServersAreAvailable()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        _ = await controller.RunOnceAsync();
        var response = await controller.HandleCommandAsync("CREATE", new[] { "%#General" });

        Assert.That(response.Status, Is.EqualTo("BUSY"));
        Assert.That(response.Arguments, Is.Empty);
    }

    [Test]
    public async Task HandleCommandAsync_Create_ReturnsSuccessWithAssignedServerIdAndChannelUid()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatChatServerAsync("acs-2", 5, TimeSpan.FromMinutes(1));
        await store.HeartbeatChatServerAsync("acs-1", 1, TimeSpan.FromMinutes(1));

        _ = await controller.RunOnceAsync();
        var response = await controller.HandleCommandAsync("CREATE", new[] { "%#General" });

        Assert.That(response.Status, Is.EqualTo("SUCCESS"));
        Assert.That(response.Arguments.Count, Is.EqualTo(2));
        Assert.That(response.Arguments[0], Is.EqualTo("acs-1"));
        Assert.That(response.Arguments[1], Does.StartWith("acs-1:"));
        Assert.That(response.ToProtocolString(), Does.StartWith("SUCCESS acs-1 acs-1:"));
    }

    [Test]
    public async Task RunOnce_ReconcilesAssignmentsThatPointToExpiredWorkers()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        await store.HeartbeatChatServerAsync("chat-1", 5, TimeSpan.FromMinutes(1));
        await store.HeartbeatBroadcastWorkerAsync("worker-stale", 0, TimeSpan.FromMilliseconds(1));
        await store.SetChatServerAssignmentAsync("chat-1", "worker-stale");

        await Task.Delay(20);

        _ = await controller.RunOnceAsync();
        var assignments = await store.GetChatServerAssignmentsAsync();

        Assert.That(assignments, Is.Empty);
    }
}

