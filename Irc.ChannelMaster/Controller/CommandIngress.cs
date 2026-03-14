using System.Text.Json;
using Irc.Contracts;
using Irc.Contracts.Messages;
using StackExchange.Redis;

namespace Irc.ChannelMaster.Controller;

/// <summary>
/// Subscribes to the cm:cmd:controller Redis pub-sub channel and dispatches
/// incoming commands (CREATE, FINDHOST, ASSIGN) to the ControllerProcess.
/// Writes responses back to cm:reply:{requestId} temporary keys.
///
/// This is the "front door" that connects ADS servers to the ChannelMaster controller.
/// </summary>
public sealed class CommandIngress : IDisposable
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ControllerProcess _controller;
    private ISubscriber? _subscriber;

    public CommandIngress(IConnectionMultiplexer redis, ControllerProcess controller)
    {
        _redis = redis;
        _controller = controller;
    }

    /// <summary>
    /// Starts listening for commands on cm:cmd:controller.
    /// </summary>
    public void Start(CancellationToken cancellationToken = default)
    {
        _subscriber = _redis.GetSubscriber();
        var channel = new RedisChannel(RedisChannels.ControllerCommandChannel, RedisChannel.PatternMode.Literal);

        Console.WriteLine($"[CommandIngress] Subscribing to {RedisChannels.ControllerCommandChannel}");

        _subscriber.Subscribe(channel, async (_, value) =>
        {
            if (!value.HasValue) return;

            try
            {
                await HandleMessageAsync((string)value!, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CommandIngress] Error handling command: {ex.Message}");
            }
        });

        cancellationToken.Register(() =>
        {
            try
            {
                _subscriber?.Unsubscribe(channel);
            }
            catch { }
        });
    }

    private async Task HandleMessageAsync(string payload, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<ControllerRequest>(payload);
        if (request == null)
        {
            Console.WriteLine($"[CommandIngress] Could not deserialize request: {payload}");
            return;
        }

        Console.WriteLine($"[CommandIngress] Received {request.Command} from {request.RequesterId ?? "unknown"} (reqId={request.RequestId})");

        // Dispatch through the existing controller command pipeline
        var result = await _controller.HandleCommandAsync(request.Command, request.Arguments, cancellationToken);

        // Map ControllerCommandResponse → ControllerResponse DTO
        var response = new ControllerResponse
        {
            RequestId = request.RequestId,
            Status = MapStatus(result.Status),
            Values = result.Arguments.ToArray()
        };

        // Write to the reply key
        var replyKey = RedisChannels.ControllerReplyKey(request.RequestId);
        var json = JsonSerializer.Serialize(response);
        var db = _redis.GetDatabase();
        await db.StringSetAsync(replyKey, json, RedisChannels.ReplyKeyTtl);

        Console.WriteLine($"[CommandIngress] Replied {response.Status} to {replyKey}");
    }

    /// <summary>
    /// Maps internal status strings to the well-known ControllerResponse status codes.
    /// </summary>
    private static string MapStatus(string internalStatus)
    {
        return internalStatus.ToUpperInvariant() switch
        {
            "SUCCESS" => ControllerResponse.StatusSuccess,
            "BUSY" => ControllerResponse.StatusBusy,
            "NAME CONFLICT" => ControllerResponse.StatusNameConflict,
            "NOT_FOUND" or "NOT FOUND" => ControllerResponse.StatusNotFound,
            _ => ControllerResponse.StatusError
        };
    }

    public void Dispose()
    {
        if (_subscriber != null)
        {
            try
            {
                _subscriber.UnsubscribeAll();
            }
            catch { }
        }
    }
}
