using System.Text.Json;
using Irc.Contracts;
using Irc.Contracts.Messages;
using StackExchange.Redis;

namespace Irc.Directory;

/// <summary>
/// In-memory channel directory maintained by the ADS (Channel Server).
/// Receives CHANNEL-UPDATE messages from the ChannelMaster BroadcastProcess
/// via Redis pub-sub and maintains a complete picture of all channels across
/// all ChatServers (doc section 4.4.3).
///
/// "Every update is complete and contained and does not depend on any
/// previous update." — so each CHANNEL-UPDATE for a ChatServer fully
/// replaces the ADS's view of that server's channels.
/// </summary>
public sealed class ChannelStore : IDisposable
{
    private readonly object _sync = new();

    /// <summary>
    /// Channels indexed by ChatServerId → (ChannelName (upper) → entry).
    /// Each CHANNEL-UPDATE fully replaces the inner dictionary for one ChatServer.
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, ChannelStoreEntry>> _channelsByChatServer
        = new(StringComparer.OrdinalIgnoreCase);

    private ISubscriber? _subscriber;

    /// <summary>
    /// Subscribes to the cm:channel:update pub-sub channel on the given Redis connection.
    /// Call this once after construction when Redis is available.
    /// </summary>
    public void SubscribeToChannelUpdates(IConnectionMultiplexer redis)
    {
        _subscriber = redis.GetSubscriber();
        _subscriber.Subscribe(
            RedisChannel.Literal(RedisChannels.ChannelUpdateChannel),
            (_, message) =>
            {
                if (message.IsNullOrEmpty) return;
                try
                {
                    var update = JsonSerializer.Deserialize<ChannelUpdateMessage>(message.ToString());
                    if (update != null)
                    {
                        ApplyChannelUpdate(update);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[ChannelStore] Failed to deserialize CHANNEL-UPDATE: {ex.Message}");
                }
            });
    }

    /// <summary>
    /// Applies a CHANNEL-UPDATE message. Per doc section 4.4.3 for dynamic channels:
    /// the update fully replaces the ADS's view of channels for the given ChatServer.
    /// </summary>
    public void ApplyChannelUpdate(ChannelUpdateMessage update)
    {
        if (string.IsNullOrWhiteSpace(update.ChatServerId)) return;

        lock (_sync)
        {
            if (update.Channels == null || update.Channels.Length == 0)
            {
                // Empty snapshot — this ChatServer has no channels.
                // Remove all entries for it.
                _channelsByChatServer.Remove(update.ChatServerId);
                return;
            }

            // Build a new dictionary for this ChatServer from the update
            var newEntries = new Dictionary<string, ChannelStoreEntry>(
                update.Channels.Length, StringComparer.OrdinalIgnoreCase);

            foreach (var ch in update.Channels)
            {
                if (string.IsNullOrWhiteSpace(ch.ChannelName)) continue;
                newEntries[ch.ChannelName] = new ChannelStoreEntry
                {
                    ChannelName = ch.ChannelName,
                    ChannelUid = ch.ChannelUid,
                    MemberCount = ch.MemberCount,
                    ChatServerId = update.ChatServerId
                };
            }

            _channelsByChatServer[update.ChatServerId] = newEntries;
        }
    }

    /// <summary>
    /// Returns all channels across all ChatServers as a flat list.
    /// Used for LISTX enumeration (Phase 3D).
    /// </summary>
    public IReadOnlyList<ChannelStoreEntry> GetAllChannels()
    {
        lock (_sync)
        {
            var result = new List<ChannelStoreEntry>();
            foreach (var serverChannels in _channelsByChatServer.Values)
            {
                result.AddRange(serverChannels.Values);
            }
            return result;
        }
    }

    /// <summary>
    /// Returns all channels for a specific ChatServer.
    /// </summary>
    public IReadOnlyList<ChannelStoreEntry> GetChannelsForServer(string chatServerId)
    {
        lock (_sync)
        {
            if (_channelsByChatServer.TryGetValue(chatServerId, out var entries))
            {
                return entries.Values.ToList();
            }
            return [];
        }
    }

    /// <summary>
    /// Looks up a single channel by name across all ChatServers.
    /// Returns null if not found.
    /// </summary>
    public ChannelStoreEntry? FindChannelByName(string channelName)
    {
        lock (_sync)
        {
            foreach (var serverChannels in _channelsByChatServer.Values)
            {
                if (serverChannels.TryGetValue(channelName, out var entry))
                    return entry;
            }
            return null;
        }
    }

    /// <summary>
    /// Returns the total number of channels tracked across all ChatServers.
    /// </summary>
    public int TotalChannelCount
    {
        get
        {
            lock (_sync)
            {
                int count = 0;
                foreach (var serverChannels in _channelsByChatServer.Values)
                    count += serverChannels.Count;
                return count;
            }
        }
    }

    public void Dispose()
    {
        if (_subscriber != null)
        {
            _subscriber.Unsubscribe(RedisChannel.Literal(RedisChannels.ChannelUpdateChannel));
            _subscriber = null;
        }
    }
}

/// <summary>
/// A single channel entry as stored on the ADS side.
/// Populated from CHANNEL-UPDATE messages received from the ChannelMaster.
/// </summary>
public sealed class ChannelStoreEntry
{
    public required string ChannelName { get; init; }
    public required string ChannelUid { get; init; }
    public required int MemberCount { get; init; }
    public required string ChatServerId { get; init; }
}
