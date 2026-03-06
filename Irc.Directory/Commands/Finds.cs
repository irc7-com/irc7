using Irc.Commands;
using Irc.Constants;
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

    public new void Execute(IChatFrame chatFrame)
    {
        var server = (DirectoryServer)chatFrame.Server;
        string? ip = server.ChatServerIp;
        int port = server.ChatServerPort;

        if (!server.CacheManager.IsConnected)
        {
            if (string.IsNullOrEmpty(ip) || port == 0)
            {
                // Fallback or error if no servers available
                chatFrame.User.Send(Irc.Constants.Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User, "No chat servers available"));
                return;
            }

            chatFrame.User.Send(Raws.RPL_FINDS_MSN(server, chatFrame.User, ip, port.ToString()));
            return;
        }

        var roomName = chatFrame.ChatMessage.Parameters.FirstOrDefault() ?? string.Empty;
        
        // Try to find if room exists or load balance to server with least connections
        var targetServer = server.GetTargetServerForRoom(roomName);

        if (targetServer != null)
        {
            ip = targetServer.Ip;
            port = targetServer.Port;
        }

        if (string.IsNullOrEmpty(ip) || port == 0)
        {
            // Fallback or error if no servers available
            chatFrame.User.Send(Irc.Constants.Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User, "No chat servers available"));
            return;
        }

        chatFrame.User.Send(Raws.RPL_FINDS_MSN(server, chatFrame.User, ip, port.ToString()));
    }
}