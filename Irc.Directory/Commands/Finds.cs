using Irc.Commands;
using Irc.Constants;
using Irc.Contracts.Messages;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Directory.Commands;

internal class Finds : Command, ICommand
{
    public Finds()
    {
        _requiredMinimumParameters = 1;
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    private void HandleLocalFinds(IChatFrame chatFrame)
    {
        var server = (DirectoryServer)chatFrame.Server;
        string? ip = server.ChatServerIp;
        int port = server.ChatServerPort;

        if (string.IsNullOrEmpty(ip) || port == 0)
        {
            chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User, "No chat servers available"));
            return;
        }

        chatFrame.User.Send(Raws.RPL_FINDS_MSN(server, chatFrame.User, ip, port.ToString()));
    }

    /// <summary>
    /// Routes the FINDS through the ChannelMaster controller via FINDHOST.
    /// </summary>
    private async void HandleChannelMasterFinds(IChatFrame chatFrame, string roomName)
    {
        var server = (DirectoryServer)chatFrame.Server;
        var cmClient = server.ChannelMasterClient!;

        try
        {
            var response = await cmClient.FindHostAsync(roomName);

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

                case ControllerResponse.StatusNotFound:
                    // Channel doesn't exist — tell the user
                    chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User,
                        "Channel not found"));
                    break;

                default:
                    // BUSY, ERROR, etc.
                    chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User,
                        "No chat servers available"));
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Finds] ChannelMaster error: {ex.Message}");
            chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User,
                "No chat servers available"));
        }
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = (DirectoryServer)chatFrame.Server;

        if (!server.CacheManager.IsConnected)
        {
            HandleLocalFinds(chatFrame);
            return;
        }

        var roomName = chatFrame.ChatMessage.Parameters.FirstOrDefault() ?? string.Empty;

        if (server.ChannelMasterClient != null)
        {
            HandleChannelMasterFinds(chatFrame, roomName);
        }
        else
        {
            // ChannelMaster is required when Redis is connected — no fallback
            chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User,
                "No chat servers available"));
        }
    }
}