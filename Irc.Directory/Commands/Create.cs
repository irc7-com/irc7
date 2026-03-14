using Irc.Commands;
using Irc.Constants;
using Irc.Contracts.Messages;
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

    /// <summary>
    /// Routes the CREATE through the ChannelMaster controller.
    /// The ChannelMaster selects the target ACS server and tells it to host the channel.
    /// </summary>
    private async void HandleChannelMasterCreate(IChatFrame chatFrame, InMemoryChannel inMemoryChannel)
    {
        var server = (DirectoryServer)chatFrame.Server;
        var cmClient = server.ChannelMasterClient!;

        try
        {
            var response = await cmClient.CreateAsync(inMemoryChannel.ChannelName);

            if (response == null)
            {
                // Timeout or no ChannelMaster listening
                chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User,
                    "No chat servers available"));
                return;
            }

            switch (response.Status)
            {
                case ControllerResponse.StatusSuccess:
                    // Hostname is "ip:port"
                    var hostname = response.Hostname ?? string.Empty;
                    var parts = hostname.Split(':');
                    var ip = parts.Length > 0 ? parts[0] : string.Empty;
                    var port = parts.Length > 1 ? parts[1] : "6667";

                    chatFrame.User.Send(Raws.RPL_FINDS_MSN(server, chatFrame.User, ip, port));
                    break;

                case ControllerResponse.StatusNameConflict:
                    chatFrame.User.Send(Raws.IRCX_RPL_FINDS_CHANNELEXISTS_705(server, chatFrame.User));
                    break;

                case ControllerResponse.StatusBusy:
                    chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User,
                        "No chat servers available"));
                    break;

                default:
                    chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User,
                        $"Channel creation failed: {response.Status}"));
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Create] ChannelMaster error: {ex.Message}");
            chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User,
                "No chat servers available"));
        }
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = (DirectoryServer)chatFrame.Server;
        if (!server.CacheManager.IsConnected)
        {
            // When not connected to Redis, use static ACS config
            HandleLocalCreate(chatFrame);
            return;
        }
        
        var channel = global::Irc.Commands.Create.ProcessCreateRequest(chatFrame);
        if (channel == null) return;

        if (server.ChannelMasterClient != null)
        {
            HandleChannelMasterCreate(chatFrame, channel);
        }
        else
        {
            // ChannelMaster is required when Redis is connected — no fallback
            chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User,
                "No chat servers available"));
        }
    }
}