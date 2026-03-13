using Irc.ChannelMaster.Controller;
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
    public async Task TryCreateChannel_IsCaseInsensitiveAndCreateOnce()
    {
        var store = new InMemoryChannelMasterStore();
        var controller = new ControllerProcess(store, "controller-A")
        {
            LeaderPollRepeats = 1,
            LeaderPollInterval = TimeSpan.Zero
        };

        var first = await controller.TryCreateChannelAsync("%#General");
        var second = await controller.TryCreateChannelAsync("%#general");
        var owner = await controller.GetChannelOwnerAsync("%#GENERAL");

        Assert.That(first, Is.True);
        Assert.That(second, Is.False);
        Assert.That(owner, Is.EqualTo("controller-A"));
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
}

