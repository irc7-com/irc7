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
        // CREATE GN %#test %An\bamazing\btopic - EN-US 1 62269 0
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

        chatFrame.User.Send(DirectoryRaws.RPL_FINDS_MSN(server, chatFrame.User, ip, port.ToString()));
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

        // Try to find if room exists or load balance to server with least connections
        var targetServer = server.GetTargetServerForRoom(inMemoryChannel.ChannelName);
        if (targetServer == null || 
            (string.IsNullOrWhiteSpace(targetServer.Ip) || targetServer.Port == 0)
           )
        {
            // Fallback or error if no server available
            chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User, "No chat servers available"));
            return;
        }

        var ip = targetServer.Ip;
        var port = targetServer.Port;
        
        inMemoryChannel.ServerName = targetServer.Name;

        server.CacheManager.Subscriber.Publish(
            "service",
            JsonSerializer.Serialize(inMemoryChannel)
        );
        chatFrame.User.Send(DirectoryRaws.RPL_FINDS_MSN(server, chatFrame.User, ip, port.ToString()));
    }

    public new void Execute(IChatFrame chatFrame)
    {
        if (!Channel.IsAllowedCategory(chatFrame.ChatMessage.Parameters[0]))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_FINDS_NOSUCHCAT_701(chatFrame.Server, chatFrame.User)
            );
            return;
        }

        if (!Channel.ValidName(chatFrame.ChatMessage.Parameters[1]))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_FINDS_INVALIDCHANNEL_706(chatFrame.Server, chatFrame.User)
            );
            return;
        }

        if (!Channel.IsModeSupported(chatFrame.Server, chatFrame.ChatMessage.Parameters[3]))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_FINDS_INVALIDMODE_706(chatFrame.Server, chatFrame.User)
            );
            return;
        }

        if (!Channel.IsAllowedRegion(chatFrame.ChatMessage.Parameters[4]))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_CREATE_INVALIDREGION_706(chatFrame.Server, chatFrame.User)
            );
            return;
        }
        
        /*
         * Currently missing validation for
         * Topic
         * Language
         * Ownerkey
         * 0 fixed part at the end
         */
        
        var server = (DirectoryServer)chatFrame.Server;
        if (!server.CacheManager.IsConnected)
        {
            // At the moment this is just a dumb return of 613
            HandleLocalCreate(chatFrame);
            return;
        }
        
        int unknownValue;
        int.TryParse(chatFrame.ChatMessage.Parameters[7], out unknownValue);

        var channel = new InMemoryChannel
        {
            Category = chatFrame.ChatMessage.Parameters[0],
            ChannelName = chatFrame.ChatMessage.Parameters[1],
            ChannelTopic = chatFrame.ChatMessage.Parameters[2],
            Modes = chatFrame.ChatMessage.Parameters[3],
            Region = chatFrame.ChatMessage.Parameters[4],
            Language = chatFrame.ChatMessage.Parameters[5],
            OwnerKey = chatFrame.ChatMessage.Parameters[6],
            Unknown = unknownValue,
        };
        
        // Not connected to redis so send parameterized ACS
        HandleRemoteCreate(chatFrame, channel);
    }
}