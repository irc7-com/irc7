using Irc.ChannelMaster.Controller.Commands;
using Irc.ChannelMaster.Gateway;
using Irc.ChannelMaster.Models;
using Irc.ChannelMaster.State;

namespace Irc.ChannelMaster.Controller;

public sealed class ControllerProcess
{
    private readonly IChannelMasterStore _store;
    private readonly IChatServerGateway _gateway;
    private readonly Dictionary<string, IControllerCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
    private int _missedLeaderHeartbeats;
    private bool _hasObservedLeader;
    private DateTime _lastLeaderHeartbeatSentUtc = DateTime.MinValue;

    public ControllerProcess(IChannelMasterStore store, IChatServerGateway gateway, string controllerId)
    {
        _store = store;
        _gateway = gateway;
        ControllerId = controllerId;

        AddCommand(new CreateCommand());
        AddCommand(new AssignCommand());
        AddCommand(new FindHostCommand());
    }

    public void AddCommand(IControllerCommand command)
    {
        _commands[command.Name] = command;
    }

    public string ControllerId { get; }
    public TimeSpan LeaseTtl { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan ChannelMasterHeartbeatTtl { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan LeaderHeartbeatInterval { get; set; } = TimeSpan.FromSeconds(10);
    public int LeaderHeartbeatMissThreshold { get; set; } = 6;
    public int LeaderPollRepeats { get; set; } = 5;
    public TimeSpan LeaderPollInterval { get; set; } = TimeSpan.FromSeconds(3);
    public TimeSpan DefaultChannelTtl { get; set; } = TimeSpan.FromSeconds(30);
    public bool IsLeader { get; private set; }

    public IChannelMasterStore Store => _store;
    public IChatServerGateway Gateway => _gateway;

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

        // Reconcile channels reported by Chat Servers that are not yet tracked.
        // This covers default channels created at ACS startup and channels that
        // survive a ChannelMaster restart.
        await ReconcileReportedChannelsAsync(chatServers, cancellationToken);

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
        if (!_commands.TryGetValue(commandName, out var command))
        {
            return ControllerCommandResponse.Error("UNKNOWN", "COMMAND");
        }

        return await command.ExecuteAsync(this, arguments, cancellationToken);
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

        var sortedServers = chatServers
            .Where(chat => chat.Status == ChatServerStatusType.Active)
            .OrderBy(chat => chat.CurrentLoad)
            .ThenBy(chat => chat.ChatServerId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sortedServers.Count == 0)
        {
            return CreateChannelResult.Busy();
        }

        foreach (var server in sortedServers)
        {
            var now = DateTime.UtcNow;
            var channelUid = $"{server.ChatServerId}:{now.Ticks}";

            var claimed = await _store.TryClaimChannelAsync(channelName, channelUid, server.ChatServerId, now, cancellationToken);
            if (!claimed)
            {
                return CreateChannelResult.NameConflict();
            }

            var accepted = await _gateway.SendAssignAsync(server.ChatServerId, channelName, channelUid, DefaultChannelTtl, cancellationToken);
            if (accepted)
            {
                return CreateChannelResult.Success(server.ChatServerId, channelUid);
            }

            // Server refused (BUSY) — unclaim and try next server
            await _store.UnclaimChannelAsync(channelName, cancellationToken);
        }

        return CreateChannelResult.Busy();
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

    /// <summary>
    /// Handles an inbound ASSIGN request (doc 4.1.2, first table).
    /// A Channel Server asks the Controller to assign a registered channel to a Chat Server.
    /// The Controller picks the least-loaded active server and sends an outbound ASSIGN.
    /// </summary>
    public async Task<AssignChannelResult> AssignChannelAsync(string channelUid, CancellationToken cancellationToken = default)
    {
        if (!IsLeader)
        {
            return AssignChannelResult.NotLeader();
        }

        if (!await EnsureControllerLeaseAsync(cancellationToken))
        {
            IsLeader = false;
            return AssignChannelResult.NotLeader();
        }

        var channel = await _store.GetChannelByUidAsync(channelUid, cancellationToken);
        if (channel == null)
        {
            return AssignChannelResult.NotFound();
        }

        var chatServers = await _store.GetActiveChatServersAsync(cancellationToken);
        var sortedServers = chatServers
            .Where(chat => chat.Status == ChatServerStatusType.Active)
            .OrderBy(chat => chat.CurrentLoad)
            .ThenBy(chat => chat.ChatServerId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var server in sortedServers)
        {
            var accepted = await _gateway.SendAssignAsync(
                server.ChatServerId, channel.ChannelName, channel.ChannelUid, DefaultChannelTtl, cancellationToken);
            if (accepted)
            {
                return AssignChannelResult.Success(server.ChatServerId);
            }
        }

        return AssignChannelResult.Busy();
    }

    /// <summary>
    /// Handles a FINDHOST request (doc 4.1.3).
    /// Looks up which Chat Server hosts the given channel and returns its hostname.
    /// FINDHOST does not require leader status — any controller can serve it.
    /// </summary>
    public async Task<string?> FindHostAsync(string channelName, CancellationToken cancellationToken = default)
    {
        var channel = await _store.GetChannelRecordAsync(channelName, cancellationToken);
        if (channel == null)
        {
            return null;
        }

        var server = await _store.GetChatServerAsync(channel.OwnerServerId, cancellationToken);
        return server?.Hostname;
    }

    /// <summary>
    /// Reconciles channels that Chat Servers report hosting but that are not
    /// yet tracked in the ChannelMaster store. This handles two cases:
    /// 1. Default channels created at ACS startup (never went through CREATE).
    /// 2. Channels that survive a ChannelMaster restart (store was cleared).
    /// The leader claims each unreported channel on behalf of the ACS that hosts it.
    /// </summary>
    internal async Task ReconcileReportedChannelsAsync(
        IReadOnlyList<ChatServerStatus> chatServers,
        CancellationToken cancellationToken = default)
    {
        foreach (var server in chatServers)
        {
            if (server.ChannelNames.Length == 0) continue;

            foreach (var channelName in server.ChannelNames)
            {
                if (string.IsNullOrWhiteSpace(channelName)) continue;

                var existing = await _store.GetChannelRecordAsync(channelName, cancellationToken);
                if (existing != null) continue;

                // Channel exists on ACS but not in our store — claim it
                var now = DateTime.UtcNow;
                var channelUid = $"{server.ChatServerId}:{now.Ticks}";
                var claimed = await _store.TryClaimChannelAsync(
                    channelName, channelUid, server.ChatServerId, now, cancellationToken);

                if (claimed)
                {
                    Console.WriteLine(
                        $"[Controller] Reconciled channel {channelName} → {server.ChatServerId} (uid={channelUid})");
                }
            }
        }
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

