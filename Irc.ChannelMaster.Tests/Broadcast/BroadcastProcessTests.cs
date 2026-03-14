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
}

