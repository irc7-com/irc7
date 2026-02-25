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
        var messageToSend = Raws.IRCX_RPL_FINDS_613(chatFrame.Server, chatFrame.User);
        if (_isAds)
        {
            var server = (DirectoryServer)chatFrame.Server;
            string? ip = server.ChatServerIp;
            string? port = server.ChatServerPort;

            if (server.CacheManager.IsConnected)
            {
                var roomName = chatFrame.ChatMessage.Parameters.FirstOrDefault() ?? string.Empty;
                
                // Try to find if room exists
                var existingServerId = server.CacheManager.GetServerForRoom(roomName);
                Irc.Services.AcsServerInfo? targetServer = null;

                if (!string.IsNullOrEmpty(existingServerId))
                {
                    targetServer = server.CacheManager.GetActiveServers()
                        .FirstOrDefault(s => s.ServerId == existingServerId);
                }

                // Load balance to server with least connections
                if (targetServer == null)
                {
                    targetServer = server.CacheManager.GetActiveServers()
                        .OrderBy(s => s.UsersOnline)
                        .FirstOrDefault();
                }

                if (targetServer != null)
                {
                    ip = targetServer.Ip;
                    port = targetServer.Port.ToString();
                }
            }

            if (string.IsNullOrEmpty(ip) || string.IsNullOrEmpty(port))
            {
                // Fallback or error if no servers available
                chatFrame.User.Send(Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User, "No chat servers available"));
                return;
            }

            messageToSend = DirectoryRaws.RPL_FINDS_MSN(server, chatFrame.User, ip, port);
        }

        chatFrame.User.Send(messageToSend);
    }
}