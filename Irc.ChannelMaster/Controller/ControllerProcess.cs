using Irc.ChannelMaster.Models;
using Irc.ChannelMaster.State;

namespace Irc.ChannelMaster.Controller;

public sealed class ControllerProcess
{
    private readonly IChannelMasterStore _store;
    private int _missedLeaderHeartbeats;
    private bool _hasObservedLeader;
    private DateTime _lastLeaderHeartbeatSentUtc = DateTime.MinValue;

    public ControllerProcess(IChannelMasterStore store, string controllerId)
    {
        _store = store;
        ControllerId = controllerId;
    }

    public string ControllerId { get; }
    public TimeSpan LeaseTtl { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan ChannelMasterHeartbeatTtl { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan LeaderHeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10);
    public int LeaderHeartbeatMissThreshold { get; set; } = 6;
    public int LeaderPollRepeats { get; set; } = 5;
    public TimeSpan LeaderPollInterval { get; set; } = TimeSpan.FromSeconds(3);
    public bool IsLeader { get; private set; }

    public async Task<bool> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        await _store.HeartbeatChannelMasterAsync(ControllerId, ChannelMasterHeartbeatTtl, cancellationToken);

        var currentLeader = await _store.GetCurrentLeaderAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(currentLeader))
        {
            _hasObservedLeader = true;
            _missedLeaderHeartbeats = 0;
            IsLeader = string.Equals(currentLeader, ControllerId, StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            _missedLeaderHeartbeats++;
            var shouldElect = !_hasObservedLeader || _missedLeaderHeartbeats >= LeaderHeartbeatMissThreshold;
            if (shouldElect)
            {
                IsLeader = await RunElectionAsync(cancellationToken);
            }
            else
            {
                IsLeader = false;
            }
        }

        if (!IsLeader) return false;

        var now = DateTime.UtcNow;
        if (now - _lastLeaderHeartbeatSentUtc >= LeaderHeartbeatInterval)
        {
            await _store.DefineLeaderAsync(ControllerId, LeaseTtl, cancellationToken);
            _lastLeaderHeartbeatSentUtc = now;
        }

        var workers = await _store.GetActiveBroadcastWorkersAsync(cancellationToken);
        if (workers.Count == 0) return true;

        var chatServers = await _store.GetActiveChatServersAsync(cancellationToken);
        if (chatServers.Count == 0) return true;

        var trackedLoads = workers.ToDictionary(
            worker => worker.WorkerId,
            worker => worker.CurrentLoad,
            StringComparer.OrdinalIgnoreCase
        );

        var orderedChatServers = chatServers
            .OrderByDescending(chat => chat.CurrentLoad)
            .ThenBy(chat => chat.ChatServerId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var chatServer in orderedChatServers)
        {
            var targetWorker = trackedLoads
                .OrderBy(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
                .First().Key;

            await _store.SetChatServerAssignmentAsync(chatServer.ChatServerId, targetWorker, cancellationToken);
            trackedLoads[targetWorker] += Math.Max(1, chatServer.CurrentLoad);
        }

        return true;
    }

    private async Task<bool> RunElectionAsync(CancellationToken cancellationToken)
    {
        var responders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ControllerId };

        for (var attempt = 0; attempt < LeaderPollRepeats; attempt++)
        {
            var activeMembers = await _store.GetActiveChannelMastersAsync(cancellationToken);
            foreach (var member in activeMembers)
            {
                responders.Add(member);
            }

            if (attempt < LeaderPollRepeats - 1)
            {
                await Task.Delay(LeaderPollInterval, cancellationToken);
            }
        }

        var electedLeader = responders
            .OrderBy(member => member, StringComparer.OrdinalIgnoreCase)
            .Last();

        // Equivalent to LEADER-DEFINE: cluster converges on the max-ID winner.
        await _store.DefineLeaderAsync(electedLeader, LeaseTtl, cancellationToken);
        _hasObservedLeader = true;
        _missedLeaderHeartbeats = 0;

        return string.Equals(electedLeader, ControllerId, StringComparison.OrdinalIgnoreCase);
    }

    public Task<bool> TryCreateChannelAsync(string channelName, CancellationToken cancellationToken = default)
    {
        return _store.TryClaimChannelAsync(channelName, ControllerId, cancellationToken);
    }

    public Task<string?> GetChannelOwnerAsync(string channelName, CancellationToken cancellationToken = default)
    {
        return _store.GetChannelOwnerAsync(channelName, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsLeader) return;

        await _store.ReleaseControllerLeaseAsync(ControllerId, cancellationToken);
        IsLeader = false;
    }
}

