using System.Text.Json;
using System.Text.Json.Serialization;
using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;

namespace Irc.Directory.Commands;

public class Create : Command, ICommand
{
    public Create()
    {
        // IRC3
        // CREATE <catcode> <channel> <topic> <mode> [limit] <locale> <hostkey> <ownerkey>
        // IRC4, IRC5
        //  CREATE <catcode> <channel> <topic> <mode> [limit] <locale> <language> <hostkey> <ownerkey>
        // IRC7+
        // CREATE <category> <channel> <topic> <mode> [limit] <locale> <language>
        // <ownerkey> <radio station> [hostkey]
        // The radio station was obsoleted and hence is 0 (assumption)
        
        // Limit is only considered when 'l' exists in <mode>
        // Hostkey is optional
        
        // CREATE CP %#channel %topic ntl 50 EN-US 1 ownerkey 0
        // CREATE UL %#unknown - - EN-US 1 ownerkey 0
        _requiredMinimumParameters = 8;
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    private void HandleLocalCreate(IChatFrame chatFrame)
    {
        var server = (DirectoryServer)chatFrame.Server;
        string ip = server.ChatServerIp;
        int port = server.ChatServerPort;

        // Not connected to redis so send parameterized ACS
        if (string.IsNullOrEmpty(ip) || port == 0)
        {
            // Fallback or error if no servers available
            chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User, "No chat servers available"));
            return;
        }

        chatFrame.User.Send(Raws.RPL_FINDS_MSN(server, chatFrame.User, ip, port.ToString()));
    }

    private void HandleRemoteCreate(IChatFrame chatFrame, InMemoryChannel inMemoryChannel)
    {
        var server = (DirectoryServer)chatFrame.Server;
        var serverId = server.CacheManager.GetServerForRoom(inMemoryChannel.ChannelName);
        if (!string.IsNullOrWhiteSpace(serverId))
        {
            // Channel already exists
            chatFrame.User.Send(Raws.IRCX_RPL_FINDS_CHANNELEXISTS_705(server, chatFrame.User));
            return;
        }

        // Register the channel
        var targetServer = server.RegisterChannel(server.CacheManager.GetActiveServers().ToList(), inMemoryChannel);
        if (targetServer == null || 
            (string.IsNullOrWhiteSpace(targetServer.Ip) || targetServer.Port == 0)
           )
        {
            // Fallback or error if no server available
            chatFrame.User.Send(Raws.IRCX_RPL_FINDS_DOWN_703(chatFrame.Server, chatFrame.User));
            return;
        }

        inMemoryChannel.ServerName = targetServer.Name;
        
        chatFrame.User.Send(Raws.RPL_FINDS_MSN(server, chatFrame.User, targetServer.Ip, targetServer.Port.ToString()));
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = (DirectoryServer)chatFrame.Server;
        if (!server.CacheManager.IsConnected)
        {
            // When not connected to PubSub
            // At the moment this is just a dumb return of 613
            HandleLocalCreate(chatFrame);
            return;
        }
        
        var channel = global::Irc.Commands.Create.ProcessCreateRequest(chatFrame);
        
        // Handle via PubSub
        if (channel != null) HandleRemoteCreate(chatFrame, channel);
    }
}