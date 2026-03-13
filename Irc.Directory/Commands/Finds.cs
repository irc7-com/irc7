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

        if (!server.CacheManager.IsConnected)
        {
            if (string.IsNullOrEmpty(server.ChatServerIp) || server.ChatServerPort == 0)
            {
                // Fallback or error if no servers available
                chatFrame.User.Send(Irc.Constants.Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User, "No chat servers available"));
                return;
            }

            chatFrame.User.Send(Raws.RPL_FINDS_MSN(server, chatFrame.User, server.ChatServerIp, server.ChatServerPort.ToString()));
            return;
        }

        var roomName = chatFrame.ChatMessage.Parameters.FirstOrDefault() ?? string.Empty;

        if (!server.CacheManager.GetActiveServers().Any())
        {
            chatFrame.User.Send(Raws.IRCX_RPL_FINDS_DOWN_703(chatFrame.Server, chatFrame.User));
            return;
        }

        // We have active servers; if the channel is not found, it does not exist
        var targetServer = server.FindChannel(roomName);
        if (targetServer == null)
        {
            chatFrame.User.Send(Irc.Constants.Raws.IRCX_RPL_FINDS_NOTFOUND_702(chatFrame.Server, chatFrame.User));
            return;
        }

        if (string.IsNullOrEmpty(targetServer.Ip) || targetServer.Port == 0)
        {
            // Fallback or error if no servers available
            chatFrame.User.Send(Irc.Constants.Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User, "No chat servers available"));
            return;
        }

        chatFrame.User.Send(Raws.RPL_FINDS_MSN(server, chatFrame.User, targetServer.Ip, targetServer.Port.ToString()));
    }
}