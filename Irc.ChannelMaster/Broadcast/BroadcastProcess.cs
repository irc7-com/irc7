using Irc.ChannelMaster.Models;
using Irc.ChannelMaster.State;

namespace Irc.ChannelMaster.Broadcast;

public sealed class BroadcastProcess
{
    private readonly IChannelMasterStore _store;

    public BroadcastProcess(IChannelMasterStore store, string workerId)
    {
        _store = store;
        WorkerId = workerId;
    }

    public string WorkerId { get; }
    public TimeSpan WorkerTtl { get; set; } = TimeSpan.FromSeconds(15);

    public async Task<BroadcastAssignmentSnapshot> RunOnceAsync(int currentLoad, CancellationToken cancellationToken = default)
    {
        await _store.HeartbeatBroadcastWorkerAsync(WorkerId, currentLoad, WorkerTtl, cancellationToken);

        var assignments = await _store.GetChatServerAssignmentsAsync(cancellationToken);
        var assignedChatServers = assignments
            .Where(kvp => kvp.Value.Equals(WorkerId, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new BroadcastAssignmentSnapshot
        {
            WorkerId = WorkerId,
            ReportedLoad = currentLoad,
            ChatServerIds = assignedChatServers
        };
    }
}

