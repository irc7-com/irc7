using Irc.Constants;
using Irc.Objects.Channel;
using NLog;
using StackExchange.Redis;

namespace Irc.Objects.Server;

public static class ServerHandlers
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    public static RedisChannel ChannelPubSub = new RedisChannel(Resources.PubSubServiceChannels, RedisChannel.PatternMode.Literal); 
    
    public static void HandleChannelPubSub(Server server, string payload)
    {
        Log.Trace($"HandleChannelPubSub: {payload}");
        var inMemoryChannel = System.Text.Json.JsonSerializer.Deserialize(payload, IrcJsonContext.Default.InMemoryChannel);
        if (inMemoryChannel == null)
        {
            Log.Error($"Could not deserialize payload: {payload}");
            return;
        }
                
        if (inMemoryChannel.ServerName == server.Name)
        {
            if (server.GetChannelByName(inMemoryChannel.ChannelName) != null)
            {
                Log.Info($"Channel {inMemoryChannel.ChannelName} already exists in memory. Skipping creation from PubSub.");
                return;
            }

            var channel = Channel.Channel.FromInMemoryChannel(inMemoryChannel);
            if (!server.AddChannel(channel))
            {
                Console.WriteLine($"Could not register channel {inMemoryChannel.ChannelName} in Redis");
                return;
            }
            Console.WriteLine($"Registered Channel {System.Text.Json.JsonSerializer.Serialize(inMemoryChannel, IrcJsonContext.Default.InMemoryChannel)} in Redis");
        }
    }
}