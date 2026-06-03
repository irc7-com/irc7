using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;

namespace Irc.Commands;

public class Kill : Command, ICommand
{
    public Kill() : base(1)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Standard;
    }

    // Exchange 5.5 KILL <nickname/channel> [<comment>]
    public new void Execute(IChatFrame chatFrame)
    {
        var server = chatFrame.Server;
        var user = chatFrame.User;
        var target = chatFrame.ChatMessage.Parameters.First();
        var reason = string.Empty;
        
        // ERR_NOPRIVILEGES: caller must be at least Sysop
        if (user.GetLevel() < EnumUserAccessLevel.Sysop)
        {
            user.Send(Raws.IRCX_ERR_NOPRIVILEGES_481(server, user));
            return;
        }
        
        if (chatFrame.ChatMessage.Parameters.Count > 1)
        {
            reason = chatFrame.ChatMessage.Parameters[1];
        }
        
        // If target is a channel (prefix: #, %, &), kill the channel
        if (Channel.ValidName(target))
        {
            KillChannel(server, target, user, reason);
            return;
        }

        // Otherwise target must be treated as a user
        KillUser(server, target, user, reason);
    }

    private static void KillChannel(IServer server, string target, IUser user, string reason)
    {
        var channel = server.GetChannelByName(target);

        if (channel == null)
        {
            user.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(server, user, target));
            return;
        }

        var members = channel.GetMembers().ToList();

        // Notify and disconnect every member
        foreach (var member in members)
        {
            var targetUser = member.GetUser();
            targetUser.Send(Raws.RPL_SERVER_KICK_IRC(channel, targetUser, reason));
            targetUser.RemoveChannel(channel);
        }

        // Remove the channel from the server (also synchronizes via Redis Cache Manager)
        server.RemoveChannel(channel);
    }

    private static void KillUser(IServer server, string target, IUser user, string reason)
    {
        var targetUsers = server.GetUsers()
            .Where(targetUser => NicknameMatches(targetUser, target))
            .ToList();

        if (targetUsers.Count == 0)
        {
            user.Send(Raws.IRCX_ERR_NOSUCHNICK_401(server, user, target));
            return;
        }

        // Prevent killing a user with equal or higher privileges
        if (targetUsers.Any(targetUser => targetUser.GetLevel() >= user.GetLevel()))
        {
            user.Send(Raws.IRCX_ERR_SECURITY_908(server, user));
            return;
        }

        // Remove all matching nickname connections from every channel, then disconnect
        foreach (var targetUser in targetUsers)
        {
            foreach (var (channel, member) in targetUser.GetChannels().ToList())
            {
                targetUser.RemoveChannel(channel);
                channel.GetMembers().Remove(member);
                channel.Send(Raws.RPL_KILL_IRC(user, targetUser, reason));
            }

            targetUser.Disconnect(
                Raws.IRCX_CLOSINGLINK_007_SYSTEMKILL(server, targetUser, targetUser.GetAddress().RemoteIp));
        }
    }

    private static bool NicknameMatches(IUser user, string nickname)
    {
        return string.Equals(user.GetAddress().Nickname.Trim(), nickname.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}