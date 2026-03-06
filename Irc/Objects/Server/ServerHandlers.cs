using System.Text.Json;
using Irc.Objects.Channel;
using NLog;

namespace Irc.Objects.Server;

public static class ServerHandlers
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    public static void HandleChannelPubSub(Server server, string payload)
    {
        Log.Trace($"HandleChannelPubSub: {payload}");
        var inMemoryChannel = JsonSerializer.Deserialize<InMemoryChannel>(payload);
        if (inMemoryChannel == null)
        {
            Log.Error($"Could not deserialize payload: {payload}");
            return;
        }
                
        if (inMemoryChannel.ServerName == server.Name)
        {
            var channel = Channel.Channel.FromInMemoryChannel(inMemoryChannel);
            if (!server.AddChannel(channel))
            {
                Console.WriteLine($"Could not register channel {inMemoryChannel.ChannelName} in Redis");
                return;
            }
            Console.WriteLine($"Registered Channel {JsonSerializer.Serialize(inMemoryChannel)} in Redis");
        }
    }
}