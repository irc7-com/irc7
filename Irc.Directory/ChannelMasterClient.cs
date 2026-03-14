using System.Text.Json;
using Irc.Contracts;
using Irc.Contracts.Messages;
using StackExchange.Redis;

namespace Irc.Directory;

/// <summary>
/// Client used by ADS (Directory Server) to send commands to the ChannelMaster controller
/// via Redis pub-sub and poll for responses on temporary keys.
///
/// Flow:
///   1. Publish ControllerRequest JSON to cm:cmd:controller
///   2. Poll cm:reply:{requestId} until a response appears or timeout
///   3. Return the ControllerResponse
/// </summary>
public sealed class ChannelMasterClient
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _replyTimeout;
    private readonly TimeSpan _pollInterval;
    private readonly string? _requesterId;

    public ChannelMasterClient(
        IConnectionMultiplexer redis,
        string? requesterId = null,
        TimeSpan? replyTimeout = null,
        TimeSpan? pollInterval = null)
    {
        _redis = redis;
        _requesterId = requesterId;
        _replyTimeout = replyTimeout ?? RedisChannels.ReplyTimeout;
        _pollInterval = pollInterval ?? RedisChannels.ReplyPollInterval;
    }

    /// <summary>
    /// Sends a CREATE command and returns the response.
    /// </summary>
    public Task<ControllerResponse?> CreateAsync(string channelName, CancellationToken cancellationToken = default)
    {
        return SendCommandAsync("CREATE", [channelName], cancellationToken);
    }

    /// <summary>
    /// Sends a FINDHOST command and returns the response.
    /// </summary>
    public Task<ControllerResponse?> FindHostAsync(string channelName, CancellationToken cancellationToken = default)
    {
        return SendCommandAsync("FINDHOST", [channelName], cancellationToken);
    }

    /// <summary>
    /// Sends an ASSIGN command and returns the response.
    /// </summary>
    public Task<ControllerResponse?> AssignAsync(string channelUid, CancellationToken cancellationToken = default)
    {
        return SendCommandAsync("ASSIGN", [channelUid], cancellationToken);
    }

    /// <summary>
    /// Sends a command to the ChannelMaster controller and waits for a response.
    /// Returns null on timeout.
    /// </summary>
    public async Task<ControllerResponse?> SendCommandAsync(
        string command,
        string[] arguments,
        CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid().ToString("N");

        var request = new ControllerRequest
        {
            RequestId = requestId,
            Command = command,
            Arguments = arguments,
            RequesterId = _requesterId
        };

        var json = JsonSerializer.Serialize(request);
        var replyKey = RedisChannels.ControllerReplyKey(requestId);

        // Publish command to the controller channel
        var subscriber = _redis.GetSubscriber();
        var receivers = await subscriber.PublishAsync(
            RedisChannel.Literal(RedisChannels.ControllerCommandChannel), json);

        // If nobody is listening, no ChannelMaster controller is running
        if (receivers == 0)
        {
            Console.WriteLine($"[ChannelMasterClient] No ChannelMaster controller listening for {command}");
            return null;
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

                return JsonSerializer.Deserialize<ControllerResponse>((string)value!);
            }

            await Task.Delay(_pollInterval, cancellationToken);
        }

        Console.WriteLine($"[ChannelMasterClient] Timeout waiting for {command} response (reqId={requestId})");
        return null;
    }
}
