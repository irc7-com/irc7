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

        if (!await EnsureControllerLeaseAsync(cancellationToken))
        {
            IsLeader = false;
            return false;
        }

        var now = DateTime.UtcNow;
        if (now - _lastLeaderHeartbeatSentUtc >= LeaderHeartbeatInterval)
        {
            await _store.DefineLeaderAsync(ControllerId, LeaseTtl, cancellationToken);
            _lastLeaderHeartbeatSentUtc = now;
        }

        var workers = await _store.GetActiveBroadcastWorkersAsync(cancellationToken);
        var chatServers = await _store.GetActiveChatServersAsync(cancellationToken);
        await _store.ReconcileChatServerAssignmentsAsync(cancellationToken);

        if (workers.Count == 0 || chatServers.Count == 0) return true;

        var currentAssignments = await _store.GetChatServerAssignmentsAsync(cancellationToken);

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

            if (!currentAssignments.TryGetValue(chatServer.ChatServerId, out var assignedWorker) ||
                !assignedWorker.Equals(targetWorker, StringComparison.OrdinalIgnoreCase))
            {
                await _store.SetChatServerAssignmentAsync(chatServer.ChatServerId, targetWorker, cancellationToken);
            }

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
        if (string.Equals(electedLeader, ControllerId, StringComparison.OrdinalIgnoreCase) &&
            !await EnsureControllerLeaseAsync(cancellationToken))
        {
            return false;
        }

        await _store.DefineLeaderAsync(electedLeader, LeaseTtl, cancellationToken);
        _hasObservedLeader = true;
        _missedLeaderHeartbeats = 0;

        return string.Equals(electedLeader, ControllerId, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ControllerCommandResponse> HandleCommandAsync(
        string commandName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        if (commandName.Equals("CREATE", StringComparison.OrdinalIgnoreCase))
        {
            if (arguments.Count != 1 || string.IsNullOrWhiteSpace(arguments[0]))
            {
                return ControllerCommandResponse.Error("CREATE", "REQUIRES", "1", "ARGUMENT");
            }

            var result = await CreateChannelAsync(arguments[0], cancellationToken);
            return result.Status switch
            {
                CreateChannelStatus.Success => ControllerCommandResponse.Success(result.ServerId!, result.ChannelUid!),
                CreateChannelStatus.Busy => ControllerCommandResponse.Busy(),
                CreateChannelStatus.NameConflict => ControllerCommandResponse.NameConflict(),
                CreateChannelStatus.NotLeader => ControllerCommandResponse.Error("NOT", "LEADER"),
                _ => ControllerCommandResponse.Error("UNKNOWN")
            };
        }

        return ControllerCommandResponse.Error("UNKNOWN", "COMMAND");
    }

    public async Task<CreateChannelResult> CreateChannelAsync(string channelName, CancellationToken cancellationToken = default)
    {
        if (!IsLeader)
        {
            return CreateChannelResult.NotLeader();
        }

        if (!await EnsureControllerLeaseAsync(cancellationToken))
        {
            IsLeader = false;
            return CreateChannelResult.NotLeader();
        }

        var chatServers = await _store.GetActiveChatServersAsync(cancellationToken);
        if (chatServers.Count == 0)
        {
            return CreateChannelResult.Busy();
        }

        var targetServer = chatServers
            .OrderBy(chat => chat.CurrentLoad)
            .ThenBy(chat => chat.ChatServerId, StringComparer.OrdinalIgnoreCase)
            .First();

        var now = DateTime.UtcNow;
        var channelUid = $"{targetServer.ChatServerId}:{now.Ticks}";

        var claimed = await _store.TryClaimChannelAsync(channelName, channelUid, targetServer.ChatServerId, now, cancellationToken);
        return claimed
            ? CreateChannelResult.Success(targetServer.ChatServerId, channelUid)
            : CreateChannelResult.NameConflict();
    }

    public async Task<bool> TryCreateChannelAsync(string channelName, CancellationToken cancellationToken = default)
    {
        var result = await CreateChannelAsync(channelName, cancellationToken);
        return result.Status == CreateChannelStatus.Success;
    }

    public Task<ChannelRecord?> GetChannelRecordAsync(string channelName, CancellationToken cancellationToken = default)
    {
        return _store.GetChannelRecordAsync(channelName, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsLeader) return;

        await _store.ReleaseControllerLeaseAsync(ControllerId, cancellationToken);
        IsLeader = false;
    }

    private async Task<bool> EnsureControllerLeaseAsync(CancellationToken cancellationToken)
    {
        if (await _store.RenewControllerLeaseAsync(ControllerId, LeaseTtl, cancellationToken))
        {
            return true;
        }

        return await _store.TryAcquireControllerLeaseAsync(ControllerId, LeaseTtl, cancellationToken);
    }
}

