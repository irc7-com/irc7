using Irc.ChannelMaster.Broadcast;
using Irc.ChannelMaster.State;

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

        var assigned = await process.RunOnceAsync(5);

        Assert.That(assigned, Is.EqualTo(new[] { "chat-1", "chat-3" }));
    }

    [Test]
    public async Task RunOnce_HeartbeatsWorker()
    {
        var store = new InMemoryChannelMasterStore();
        var process = new BroadcastProcess(store, "worker-A");

        _ = await process.RunOnceAsync(3);
        var workers = await store.GetActiveBroadcastWorkersAsync();

        Assert.That(workers.Any(w => w.WorkerId == "worker-A"), Is.True);
    }
}

