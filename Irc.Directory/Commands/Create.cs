using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Directory.Commands;

public class Create : Command, ICommand
{
    private readonly bool _isAds;

    public Create(bool isAds = false)
    {
        _requiredMinimumParameters = 1;
        _isAds = isAds;
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        if (!_isAds)
        {
            chatFrame.User.Send(Raws.IRCX_RPL_FINDS_613(chatFrame.Server, chatFrame.User));
            return;
        }

        var server = (DirectoryServer)chatFrame.Server;
        string? ip = server.ChatServerIp;
        string? port = server.ChatServerPort;

        if (!server.CacheManager.IsConnected)
        {
            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port))
            {
                // Fallback or error if no servers available
                chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User, "No chat servers available"));
                return;
            }

            chatFrame.User.Send(DirectoryRaws.RPL_FINDS_MSN(server, chatFrame.User, ip, port));
            return;
        }

        var roomName = chatFrame.ChatMessage.Parameters.FirstOrDefault() ?? string.Empty;
        
        // Try to find if room exists or load balance to server with least connections
        var targetServer = server.GetTargetServerForRoom(roomName);

        if (targetServer != null)
        {
            ip = targetServer.Ip;
            port = targetServer.Port.ToString();
        }

        if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port))
        {
            // Fallback or error if no servers available
            chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User, "No chat servers available"));
            return;
        }

        chatFrame.User.Send(DirectoryRaws.RPL_FINDS_MSN(server, chatFrame.User, ip, port));
    }
}