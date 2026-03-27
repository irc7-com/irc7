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

    /// <summary>
    /// Tracks Chat Servers that were previously active but are now missing.
    /// Key = ChatServerId, Value = number of consecutive cycles the server has been absent.
    /// After 1 missed cycle the server is Suspect (no new channels assigned).
    /// After 2 missed cycles the server is Dead (channels reassigned). (doc 4.5.3)
    /// </summary>
    private readonly Dictionary<string, int> _suspectServers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Set of Chat Server IDs that were active in the previous RunOnceAsync cycle.
    /// Used to detect newly missing servers for fail-over tracking.
    /// </summary>
    private readonly HashSet<string> _previouslyActiveServers = new(StringComparer.OrdinalIgnoreCase);

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

        // Remove channel records whose owner ACS no longer reports them
        // (the channel was deleted on the ACS, e.g., empty for >5 min).
        await CleanupOrphanedChannelsAsync(chatServers, cancellationToken);

        // Detect and handle Chat Server fail-over (doc 4.5.3):
        // servers that were previously active but are now missing from the
        // heartbeat list are tracked as Suspect → Dead and their channels
        // reassigned to surviving servers.
        await DetectAndHandleFailOverAsync(chatServers, cancellationToken);

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

    /// <summary>
    /// Removes channel records from the store whose owner ACS either:
    /// 1. Is still alive but no longer reports the channel in its heartbeat
    ///    (the channel was deleted on the ACS, e.g., empty for &gt;5 min).
    /// 2. Is dead (no longer in the active chat server list).
    /// This is the reverse of <see cref="ReconcileReportedChannelsAsync"/>:
    /// that method adds missing channels, this method removes stale ones.
    /// </summary>
    internal async Task CleanupOrphanedChannelsAsync(
        IReadOnlyList<ChatServerStatus> chatServers,
        CancellationToken cancellationToken = default)
    {
        var allRecords = await _store.GetAllChannelRecordsAsync(cancellationToken);
        if (allRecords.Count == 0) return;

        // Build a lookup: serverId → set of channel names that server reports hosting
        var reportedByServer = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var activeServerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var server in chatServers)
        {
            activeServerIds.Add(server.ChatServerId);

            var channelSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in server.ChannelNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                    channelSet.Add(name);
            }

            reportedByServer[server.ChatServerId] = channelSet;
        }

        foreach (var (_, record) in allRecords)
        {
            var ownerId = record.OwnerServerId;

            if (!activeServerIds.Contains(ownerId))
            {
                // Owner ACS is not in the active list.
                // If it's being tracked as suspect by fail-over detection, skip —
                // DetectAndHandleFailOverAsync will handle reassignment.
                // Otherwise (never-seen server, e.g. stale data after CM restart),
                // remove the orphaned channel immediately.
                if (_suspectServers.ContainsKey(ownerId) || _previouslyActiveServers.Contains(ownerId))
                    continue;

                await _store.UnclaimChannelAsync(record.ChannelName, cancellationToken);
                Console.WriteLine(
                    $"[Controller] Cleaned up orphaned channel {record.ChannelName} (owner {ownerId} is unknown)");
                continue;
            }

            // Owner is alive — check if it still reports this channel
            if (reportedByServer.TryGetValue(ownerId, out var reported) &&
                !reported.Contains(record.ChannelName))
            {
                await _store.UnclaimChannelAsync(record.ChannelName, cancellationToken);
                Console.WriteLine(
                    $"[Controller] Cleaned up stale channel {record.ChannelName} (owner {ownerId} no longer reports it)");
            }
        }
    }

    /// <summary>
    /// Detects Chat Servers that have disappeared from the active list and
    /// handles fail-over per doc section 4.5.3:
    ///   1. First missed cycle → server marked Suspect (no new channels assigned,
    ///      already handled: CreateChannelAsync filters Status == Active).
    ///   2. Second consecutive missed cycle → server declared Dead; all its
    ///      channels are reassigned to the least-loaded active Chat Server.
    ///   3. If a suspect server reappears, it is removed from tracking.
    /// </summary>
    internal async Task DetectAndHandleFailOverAsync(
        IReadOnlyList<ChatServerStatus> chatServers,
        CancellationToken cancellationToken = default)
    {
        var currentActiveIds = new HashSet<string>(
            chatServers.Select(s => s.ChatServerId), StringComparer.OrdinalIgnoreCase);

        // Servers that were previously active but are now missing
        var newlyMissing = _previouslyActiveServers
            .Where(id => !currentActiveIds.Contains(id))
            .ToList();

        // Add newly missing servers to suspect tracking
        foreach (var id in newlyMissing)
        {
            if (!_suspectServers.ContainsKey(id))
                _suspectServers[id] = 1;
        }

        // Increment miss count for already-suspect servers that are still absent
        foreach (var id in _suspectServers.Keys.ToList())
        {
            if (!currentActiveIds.Contains(id) && !newlyMissing.Contains(id))
                _suspectServers[id]++;
        }

        // Servers that reappeared — remove from suspect tracking
        var recovered = _suspectServers.Keys
            .Where(id => currentActiveIds.Contains(id))
            .ToList();
        foreach (var id in recovered)
        {
            _suspectServers.Remove(id);
            Console.WriteLine($"[Controller] Chat Server {id} recovered from suspect state");
        }

        // Process dead servers (2+ consecutive missed cycles)
        var deadServers = _suspectServers
            .Where(kvp => kvp.Value >= 2)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var deadServerId in deadServers)
        {
            Console.WriteLine($"[Controller] Chat Server {deadServerId} declared dead — reassigning channels");
            await ReassignChannelsFromDeadServerAsync(deadServerId, chatServers, cancellationToken);
            _suspectServers.Remove(deadServerId);
        }

        // Log suspect servers (1 missed cycle)
        foreach (var (id, count) in _suspectServers)
        {
            Console.WriteLine($"[Controller] Chat Server {id} is suspect (missed {count} cycle(s))");
        }

        // Update the set for the next cycle
        _previouslyActiveServers.Clear();
        foreach (var id in currentActiveIds)
            _previouslyActiveServers.Add(id);
    }

    /// <summary>
    /// Reassigns all channels owned by a dead Chat Server to surviving active servers.
    /// For each channel, the least-loaded active server is selected and ASSIGN is sent.
    /// If ASSIGN succeeds, the channel record's owner is updated.
    /// If no active server accepts, the channel is removed (unclaimed).
    /// </summary>
    internal async Task ReassignChannelsFromDeadServerAsync(
        string deadServerId,
        IReadOnlyList<ChatServerStatus> chatServers,
        CancellationToken cancellationToken = default)
    {
        var allRecords = await _store.GetAllChannelRecordsAsync(cancellationToken);
        var orphanedChannels = allRecords.Values
            .Where(r => string.Equals(r.OwnerServerId, deadServerId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (orphanedChannels.Count == 0) return;

        var activeServers = chatServers
            .Where(s => s.Status == ChatServerStatusType.Active)
            .OrderBy(s => s.CurrentLoad)
            .ThenBy(s => s.ChatServerId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var channel in orphanedChannels)
        {
            bool reassigned = false;

            foreach (var server in activeServers)
            {
                var accepted = await _gateway.SendAssignAsync(
                    server.ChatServerId, channel.ChannelName, channel.ChannelUid,
                    DefaultChannelTtl, cancellationToken);

                if (accepted)
                {
                    await _store.UpdateChannelOwnerAsync(
                        channel.ChannelName, server.ChatServerId, cancellationToken);
                    Console.WriteLine(
                        $"[Controller] Reassigned {channel.ChannelName} from {deadServerId} → {server.ChatServerId}");
                    reassigned = true;
                    break;
                }
            }

            if (!reassigned)
            {
                // No server accepted — remove the orphaned channel
                await _store.UnclaimChannelAsync(channel.ChannelName, cancellationToken);
                Console.WriteLine(
                    $"[Controller] Removed orphaned channel {channel.ChannelName} (no server accepted reassignment)");
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

