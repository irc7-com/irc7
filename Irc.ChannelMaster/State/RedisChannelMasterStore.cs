using System.Text.Json;
using Irc.ChannelMaster.Models;
using StackExchange.Redis;

namespace Irc.ChannelMaster.State;

public sealed class RedisChannelMasterStore : IChannelMasterStore, IDisposable
{
    private const string ChannelMasterSetKey = "cm:cluster:masters";
    private const string LeaderKey = "cm:cluster:leader";
    private const string ControllerLeaseKey = "cm:controller:lease";
    private const string BroadcastWorkerSetKey = "cm:broadcast:workers";
    private const string ChatServerSetKey = "cm:chat:servers";
    private const string AssignmentsKey = "cm:assign:chat-to-broadcast";
    private const string ChannelsKey = "cm:channels";

    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisChannelMasterStore(string connectionString)
    {
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _db = _redis.GetDatabase();
    }

    public async Task HeartbeatChannelMasterAsync(string channelMasterId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var key = GetChannelMasterKey(channelMasterId);
        var transaction = _db.CreateTransaction();
        _ = transaction.StringSetAsync(key, "1", ttl);
        _ = transaction.SetAddAsync(ChannelMasterSetKey, channelMasterId);
        await transaction.ExecuteAsync();
    }

    public async Task<IReadOnlyList<string>> GetActiveChannelMastersAsync(CancellationToken cancellationToken = default)
    {
        var members = await _db.SetMembersAsync(ChannelMasterSetKey);
        var active = new List<string>();

        foreach (var member in members)
        {
            var channelMasterId = member.ToString();
            if (string.IsNullOrWhiteSpace(channelMasterId)) continue;

            if (!await _db.KeyExistsAsync(GetChannelMasterKey(channelMasterId)))
            {
                await _db.SetRemoveAsync(ChannelMasterSetKey, channelMasterId);
                continue;
            }

            active.Add(channelMasterId);
        }

        return active.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<string?> GetCurrentLeaderAsync(CancellationToken cancellationToken = default)
    {
        var value = await _db.StringGetAsync(LeaderKey);
        return value.HasValue ? value.ToString() : null;
    }

    public Task DefineLeaderAsync(string leaderId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        return _db.StringSetAsync(LeaderKey, leaderId, ttl);
    }

    public async Task<bool> TryAcquireControllerLeaseAsync(string controllerId, TimeSpan leaseTtl, CancellationToken cancellationToken = default)
    {
        return await _db.StringSetAsync(ControllerLeaseKey, controllerId, leaseTtl, When.NotExists);
    }

    public async Task<bool> RenewControllerLeaseAsync(string controllerId, TimeSpan leaseTtl, CancellationToken cancellationToken = default)
    {
        var script = @"
            local current = redis.call('GET', KEYS[1])
            if current and current == ARGV[1] then
                redis.call('PEXPIRE', KEYS[1], ARGV[2])
                return 1
            end
            return 0
        ";

        var result = (int)await _db.ScriptEvaluateAsync(
            script,
            new RedisKey[] { ControllerLeaseKey },
            new RedisValue[] { controllerId, (long)leaseTtl.TotalMilliseconds }
        );

        return result == 1;
    }

    public async Task ReleaseControllerLeaseAsync(string controllerId, CancellationToken cancellationToken = default)
    {
        var script = @"
            local current = redis.call('GET', KEYS[1])
            if current and current == ARGV[1] then
                redis.call('DEL', KEYS[1])
                return 1
            end
            return 0
        ";

        await _db.ScriptEvaluateAsync(
            script,
            new RedisKey[] { ControllerLeaseKey },
            new RedisValue[] { controllerId }
        );
    }

    public async Task HeartbeatBroadcastWorkerAsync(string workerId, int currentLoad, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var key = GetBroadcastWorkerKey(workerId);
        var payload = JsonSerializer.Serialize(new BroadcastWorkerStatus
        {
            WorkerId = workerId,
            CurrentLoad = currentLoad,
            LastSeenUtc = DateTime.UtcNow
        });

        var transaction = _db.CreateTransaction();
        _ = transaction.StringSetAsync(key, payload, ttl);
        _ = transaction.SetAddAsync(BroadcastWorkerSetKey, workerId);
        await transaction.ExecuteAsync();
    }

    public async Task<IReadOnlyList<BroadcastWorkerStatus>> GetActiveBroadcastWorkersAsync(CancellationToken cancellationToken = default)
    {
        var members = await _db.SetMembersAsync(BroadcastWorkerSetKey);
        var workers = new List<BroadcastWorkerStatus>();

        foreach (var member in members)
        {
            var workerId = member.ToString();
            if (string.IsNullOrWhiteSpace(workerId)) continue;

            var payload = await _db.StringGetAsync(GetBroadcastWorkerKey(workerId));
            if (!payload.HasValue)
            {
                await _db.SetRemoveAsync(BroadcastWorkerSetKey, workerId);
                continue;
            }

            var parsed = JsonSerializer.Deserialize<BroadcastWorkerStatus>(payload.ToString());
            if (parsed != null) workers.Add(parsed);
        }

        return workers.OrderBy(w => w.WorkerId, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task HeartbeatChatServerAsync(string chatServerId, int currentLoad, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var key = GetChatServerKey(chatServerId);
        var payload = JsonSerializer.Serialize(new ChatServerStatus
        {
            ChatServerId = chatServerId,
            CurrentLoad = currentLoad,
            LastSeenUtc = DateTime.UtcNow
        });

        var transaction = _db.CreateTransaction();
        _ = transaction.StringSetAsync(key, payload, ttl);
        _ = transaction.SetAddAsync(ChatServerSetKey, chatServerId);
        await transaction.ExecuteAsync();
    }

    public async Task<IReadOnlyList<ChatServerStatus>> GetActiveChatServersAsync(CancellationToken cancellationToken = default)
    {
        var members = await _db.SetMembersAsync(ChatServerSetKey);
        var chatServers = new List<ChatServerStatus>();

        foreach (var member in members)
        {
            var chatServerId = member.ToString();
            if (string.IsNullOrWhiteSpace(chatServerId)) continue;

            var payload = await _db.StringGetAsync(GetChatServerKey(chatServerId));
            if (!payload.HasValue)
            {
                await _db.SetRemoveAsync(ChatServerSetKey, chatServerId);
                await _db.HashDeleteAsync(AssignmentsKey, chatServerId);
                continue;
            }

            var parsed = JsonSerializer.Deserialize<ChatServerStatus>(payload.ToString());
            if (parsed != null) chatServers.Add(parsed);
        }

        return chatServers.OrderBy(c => c.ChatServerId, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task<IReadOnlyDictionary<string, string>> GetChatServerAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _db.HashGetAllAsync(AssignmentsKey);
        return entries.ToDictionary(
            entry => entry.Name.ToString(),
            entry => entry.Value.ToString(),
            StringComparer.OrdinalIgnoreCase
        );
    }

    public Task SetChatServerAssignmentAsync(string chatServerId, string broadcastWorkerId, CancellationToken cancellationToken = default)
    {
        return _db.HashSetAsync(AssignmentsKey, chatServerId, broadcastWorkerId);
    }

    public async Task<bool> TryClaimChannelAsync(string channelName, string ownerId, CancellationToken cancellationToken = default)
    {
        var script = @"
            local exists = redis.call('HEXISTS', KEYS[1], ARGV[1])
            if exists == 0 then
                redis.call('HSET', KEYS[1], ARGV[1], ARGV[2])
                return 1
            end
            return 0
        ";

        var result = (int)await _db.ScriptEvaluateAsync(
            script,
            new RedisKey[] { ChannelsKey },
            new RedisValue[] { CanonicalizeChannelName(channelName), ownerId }
        );

        return result == 1;
    }

    public async Task<string?> GetChannelOwnerAsync(string channelName, CancellationToken cancellationToken = default)
    {
        var value = await _db.HashGetAsync(ChannelsKey, CanonicalizeChannelName(channelName));
        return value.HasValue ? value.ToString() : null;
    }

    public void Dispose()
    {
        _redis.Dispose();
    }

    private static string GetBroadcastWorkerKey(string workerId) => $"cm:broadcast:worker:{workerId}";
    private static string GetChannelMasterKey(string channelMasterId) => $"cm:cluster:master:{channelMasterId}";
    private static string GetChatServerKey(string chatServerId) => $"cm:chat:server:{chatServerId}";
    private static string CanonicalizeChannelName(string channelName) => channelName.ToUpperInvariant();
}

