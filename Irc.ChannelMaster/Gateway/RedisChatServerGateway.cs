using System.Text.Json;
using Irc.Contracts;
using Irc.Contracts.Messages;
using StackExchange.Redis;

namespace Irc.ChannelMaster.Gateway;

/// <summary>
/// Sends ASSIGN commands to Chat Servers via Redis pub-sub and
/// polls for their response on a temporary reply key.
///
/// Flow:
///   1. Publish AssignRequest JSON to cm:cmd:acs:{serverId}
///   2. Poll cm:reply:acs:{requestId} until a response appears or timeout
///   3. Return true (accepted) or false (busy / timeout)
/// </summary>
public sealed class RedisChatServerGateway : IChatServerGateway
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _replyTimeout;
    private readonly TimeSpan _pollInterval;

    public RedisChatServerGateway(
        IConnectionMultiplexer redis,
        TimeSpan? replyTimeout = null,
        TimeSpan? pollInterval = null)
    {
        _redis = redis;
        _replyTimeout = replyTimeout ?? RedisChannels.ReplyTimeout;
        _pollInterval = pollInterval ?? RedisChannels.ReplyPollInterval;
    }

    public async Task<bool> SendAssignAsync(
        string chatServerId,
        string channelName,
        string channelUid,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");

        var request = new AssignRequest
        {
            RequestId = requestId,
            ChannelName = channelName,
            ChannelUid = channelUid,
            TtlSeconds = (int)ttl.TotalSeconds
        };

        var json = JsonSerializer.Serialize(request);
        var channel = RedisChannels.AcsCommandChannel(chatServerId);
        var replyKey = RedisChannels.AcsReplyKey(requestId);

        // Publish ASSIGN command to the ACS's command channel
        var subscriber = _redis.GetSubscriber();
        var receivers = await subscriber.PublishAsync(RedisChannel.Literal(channel), json);

        // If nobody is listening, the ACS is unreachable — treat as BUSY
        if (receivers == 0)
        {
            return false;
        }

        // Poll for the reply key
        var db = _redis.GetDatabase();
        var deadline = DateTime.UtcNow + _replyTimeout;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var value = await db.StringGetAsync(replyKey);
            if (value.HasValue)
            {
                // Clean up the reply key
                await db.KeyDeleteAsync(replyKey);

                var response = JsonSerializer.Deserialize<AssignResponse>((string)value!);
                return response?.Accepted ?? false;
            }

            await Task.Delay(_pollInterval, cancellationToken);
        }

        // Timeout — treat as BUSY
        return false;
    }
}
