using Irc.ChannelMaster.Models;

namespace Irc.ChannelMaster.State;

public sealed class InMemoryChannelMasterStore : IChannelMasterStore
{
    private readonly object _sync = new();
    private readonly Dictionary<string, DateTime> _channelMasters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (BroadcastWorkerStatus status, DateTime expiresUtc)> _workers = new();
    private readonly Dictionary<string, (ChatServerStatus status, DateTime expiresUtc)> _chatServers = new();
    private readonly Dictionary<string, string> _assignments = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ChannelRecord> _channels = new(StringComparer.OrdinalIgnoreCase);

    private string? _leaderId;
    private DateTime _leaderExpiresUtc = DateTime.MinValue;

    private string? _controllerOwner;
    private DateTime _controllerExpiresUtc = DateTime.MinValue;

    public Task HeartbeatChannelMasterAsync(string channelMasterId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _channelMasters[channelMasterId] = DateTime.UtcNow.Add(ttl);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetActiveChannelMastersAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            PruneExpiredChannelMasters(DateTime.UtcNow);

            return Task.FromResult<IReadOnlyList<string>>(_channelMasters.Keys
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList());
        }
    }

    public Task<string?> GetCurrentLeaderAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_leaderId == null || _leaderExpiresUtc <= DateTime.UtcNow)
            {
                _leaderId = null;
                _leaderExpiresUtc = DateTime.MinValue;
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult<string?>(_leaderId);
        }
    }

    public Task DefineLeaderAsync(string leaderId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _leaderId = leaderId;
            _leaderExpiresUtc = DateTime.UtcNow.Add(ttl);
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryAcquireControllerLeaseAsync(string controllerId, TimeSpan leaseTtl, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var now = DateTime.UtcNow;
            if (_controllerOwner != null && _controllerExpiresUtc > now) return Task.FromResult(false);

            _controllerOwner = controllerId;
            _controllerExpiresUtc = now.Add(leaseTtl);
            return Task.FromResult(true);
        }
    }

    public Task<bool> RenewControllerLeaseAsync(string controllerId, TimeSpan leaseTtl, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var now = DateTime.UtcNow;
            if (_controllerOwner != controllerId || _controllerExpiresUtc <= now) return Task.FromResult(false);

            _controllerExpiresUtc = now.Add(leaseTtl);
            return Task.FromResult(true);
        }
    }

    public Task ReleaseControllerLeaseAsync(string controllerId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_controllerOwner == controllerId)
            {
                _controllerOwner = null;
                _controllerExpiresUtc = DateTime.MinValue;
            }
        }

        return Task.CompletedTask;
    }

    public Task HeartbeatBroadcastWorkerAsync(string workerId, int currentLoad, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var now = DateTime.UtcNow;
            var status = new BroadcastWorkerStatus
            {
                WorkerId = workerId,
                CurrentLoad = currentLoad,
                LastSeenUtc = now
            };

            _workers[workerId] = (status, now.Add(ttl));
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BroadcastWorkerStatus>> GetActiveBroadcastWorkersAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            PruneExpiredWorkers(DateTime.UtcNow);

            return Task.FromResult<IReadOnlyList<BroadcastWorkerStatus>>(_workers.Values
                .Select(v => v.status)
                .OrderBy(w => w.WorkerId, StringComparer.OrdinalIgnoreCase)
                .ToList());
        }
    }

    public Task HeartbeatChatServerAsync(string chatServerId, int currentLoad, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var now = DateTime.UtcNow;
            var status = new ChatServerStatus
            {
                ChatServerId = chatServerId,
                CurrentLoad = currentLoad,
                LastSeenUtc = now
            };

            _chatServers[chatServerId] = (status, now.Add(ttl));
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChatServerStatus>> GetActiveChatServersAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            PruneExpiredChatServers(DateTime.UtcNow);

            return Task.FromResult<IReadOnlyList<ChatServerStatus>>(_chatServers.Values
                .Select(v => v.status)
                .OrderBy(c => c.ChatServerId, StringComparer.OrdinalIgnoreCase)
                .ToList());
        }
    }

    public Task<IReadOnlyDictionary<string, string>> GetChatServerAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var snapshot = _assignments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
            return Task.FromResult<IReadOnlyDictionary<string, string>>(snapshot);
        }
    }

    public Task SetChatServerAssignmentAsync(string chatServerId, string broadcastWorkerId, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _assignments[chatServerId] = broadcastWorkerId;
        }

        return Task.CompletedTask;
    }

    public Task ReconcileChatServerAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var now = DateTime.UtcNow;
            PruneExpiredWorkers(now);
            PruneExpiredChatServers(now);

            var staleAssignments = _assignments
                .Where(kvp => !_chatServers.ContainsKey(kvp.Key) || !_workers.ContainsKey(kvp.Value))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var chatServerId in staleAssignments)
            {
                _assignments.Remove(chatServerId);
            }
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryClaimChannelAsync(string channelName, string channelUid, string ownerId, DateTime createdUtc, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var key = CanonicalizeChannelName(channelName);
            if (_channels.ContainsKey(key)) return Task.FromResult(false);

            _channels[key] = new ChannelRecord
            {
                ChannelUid = channelUid,
                ChannelName = channelName,
                OwnerServerId = ownerId,
                CreatedUtc = createdUtc
            };
            return Task.FromResult(true);
        }
    }

    public Task<ChannelRecord?> GetChannelRecordAsync(string channelName, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var key = CanonicalizeChannelName(channelName);
            return Task.FromResult(_channels.TryGetValue(key, out var record) ? record : null);
        }
    }

    private void PruneExpiredChannelMasters(DateTime now)
    {
        var staleIds = _channelMasters.Where(kvp => kvp.Value <= now).Select(kvp => kvp.Key).ToList();
        foreach (var staleId in staleIds) _channelMasters.Remove(staleId);
    }

    private void PruneExpiredWorkers(DateTime now)
    {
        var staleKeys = _workers.Where(kvp => kvp.Value.expiresUtc <= now).Select(kvp => kvp.Key).ToList();
        foreach (var staleKey in staleKeys) _workers.Remove(staleKey);
    }

    private void PruneExpiredChatServers(DateTime now)
    {
        var staleKeys = _chatServers.Where(kvp => kvp.Value.expiresUtc <= now).Select(kvp => kvp.Key).ToList();
        foreach (var staleKey in staleKeys)
        {
            _chatServers.Remove(staleKey);
            _assignments.Remove(staleKey);
        }
    }

    private static string CanonicalizeChannelName(string channelName) => channelName.ToUpperInvariant();
}

