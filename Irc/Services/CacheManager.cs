using System.Text.Json;
using System.Threading;
using Irc.Contracts;
using Irc.Contracts.Messages;
using StackExchange.Redis;

namespace Irc.Services;

public class CacheManager
{
    private readonly ConnectionMultiplexer? _redis;
    private readonly IDatabase? _db;
    public readonly ISubscriber? Subscriber;

    public CacheManager(string? redisUrl)
    {
        if (string.IsNullOrEmpty(redisUrl)) return;
        
        try
        {
            _redis = ConnectionMultiplexer.Connect(redisUrl);
            _db = _redis.GetDatabase();
            Subscriber = _redis.GetSubscriber();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CacheManager] Failed to connect to Redis/KeyDB at {redisUrl}: {ex.Message}");
        }
    }

    public bool IsConnected => _redis?.IsConnected ?? false;

    /// <summary>
    /// Exposes the underlying Redis connection for sharing with other components
    /// (e.g., ChannelMasterClient in the Directory Server).
    /// </summary>
    public IConnectionMultiplexer? RedisConnection => _redis;

    // ── ChannelMaster Integration ────────────────────────────────────────

    /// <summary>
    /// Heartbeats this Chat Server into the ChannelMaster's Redis keys:
    /// cm:chat:server:{serverId} (STRING with TTL) and cm:chat:servers (SET).
    /// The ChannelMaster reads these to track server health and compute load.
    /// </summary>
    public void HeartbeatToChannelMaster(string serverId, string hostname, int userCount, int channelCount, TimeSpan ttl, string[]? channelNames = null)
    {
        if (_db == null) return;

        var payload = JsonSerializer.Serialize(new ChatServerHeartbeat
        {
            ChatServerId = serverId,
            Hostname = hostname,
            UserCount = userCount,
            ChannelCount = channelCount,
            Status = ChatServerHeartbeat.StatusActive,
            LastSeenUtc = DateTime.UtcNow,
            ChannelNames = channelNames ?? []
        });

        try
        {
            var key = RedisChannels.ChatServerKey(serverId);
            var transaction = _db.CreateTransaction();
            _ = transaction.StringSetAsync(key, payload, ttl);
            _ = transaction.SetAddAsync(RedisChannels.ChatServerSet, serverId);
            transaction.Execute();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CacheManager] Failed to heartbeat to ChannelMaster: {ex.Message}");
        }
    }

    /// <summary>
    /// Removes this Chat Server from the ChannelMaster's Redis keys on shutdown.
    /// </summary>
    public void UnregisterFromChannelMaster(string serverId)
    {
        if (_db == null) return;

        try
        {
            _db.KeyDelete(RedisChannels.ChatServerKey(serverId));
            _db.SetRemove(RedisChannels.ChatServerSet, serverId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CacheManager] Failed to unregister from ChannelMaster: {ex.Message}");
        }
    }

    /// <summary>
    /// Subscribes to the ChannelMaster ASSIGN command channel for this server.
    /// The ChannelMaster publishes AssignRequest JSON to cm:cmd:acs:{serverId}.
    /// </summary>
    public void SubscribeToChannelMasterAssign(string serverId, Action<string> onMessageReceived, CancellationToken cancellationToken = default)
    {
        if (Subscriber == null) return;

        var channelName = new RedisChannel(RedisChannels.AcsCommandChannel(serverId), RedisChannel.PatternMode.Literal);

        Console.WriteLine($"[CacheManager] Subscribing to ChannelMaster ASSIGN channel {channelName}");

        try
        {
            Subscriber.Subscribe(channelName, (channel, value) =>
            {
                if (value.HasValue)
                {
                    Console.WriteLine($"[CacheManager] Received ASSIGN command on {channelName}");
                    onMessageReceived(value.ToString());
                }
            });

            cancellationToken.Register(() =>
            {
                try
                {
                    Subscriber.Unsubscribe(channelName);
                }
                catch { }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CacheManager] Failed to subscribe to ChannelMaster ASSIGN channel: {ex.Message}");
        }
    }

    /// <summary>
    /// Writes an ASSIGN response to the temporary reply key so the
    /// ChannelMaster can poll for it. Key = cm:reply:acs:{requestId}.
    /// </summary>
    public void WriteAssignResponse(string requestId, bool accepted)
    {
        if (_db == null) return;

        try
        {
            var response = new AssignResponse
            {
                RequestId = requestId,
                Accepted = accepted
            };

            var json = JsonSerializer.Serialize(response);
            var key = RedisChannels.AcsReplyKey(requestId);
            _db.StringSet(key, json, RedisChannels.ReplyKeyTtl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CacheManager] Failed to write ASSIGN response: {ex.Message}");
        }
    }

    /// <summary>
    /// Publishes a CHAT-UPDATE message to the ChannelMaster's broadcast process
    /// via the cm:chat:update pub-sub channel (doc section 4.4.5).
    /// Only channels whose member count has changed since the last update
    /// should be included. A member count of zero signals channel closure.
    /// </summary>
    public void PublishChatUpdate(string serverId, ChatUpdateEntry[] entries)
    {
        if (Subscriber == null || entries.Length == 0) return;

        try
        {
            var message = new ChatUpdateMessage
            {
                ChatServerId = serverId,
                Entries = entries
            };

            var json = JsonSerializer.Serialize(message);
            var channel = new RedisChannel(RedisChannels.ChatUpdateChannel, RedisChannel.PatternMode.Literal);
            Subscriber.Publish(channel, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CacheManager] Failed to publish CHAT-UPDATE: {ex.Message}");
        }
    }
}