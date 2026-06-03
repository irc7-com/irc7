using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

internal class Kill : Command, ICommand
{
    public Kill() : base(2)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Standard;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = chatFrame.Server;
        var user = chatFrame.User;
        var target = chatFrame.ChatMessage.Parameters.First();
        var reason = chatFrame.ChatMessage.Parameters[1];

        // ERR_NOPRIVILEGES: caller must be at least Sysop
        if (user.GetLevel() < EnumUserAccessLevel.Sysop)
        {
            user.Send(Raws.IRCX_ERR_NOPRIVILEGES_481(server, user));
            return;
        }

        var sourceChannel = user.GetChannels().FirstOrDefault().Key;
        if (sourceChannel == null)
        {
            user.Send(Raws.IRCX_ERR_U_NOTINCHANNEL_928(server, user));
            return;
        }

        var targetUserInChannel = sourceChannel.GetMembers()
            .Select(member => member.GetUser())
            .FirstOrDefault(targetUser => NicknameMatches(targetUser, target));

        if (targetUserInChannel == null)
        {
            user.Send(Raws.IRCX_ERR_NOSUCHNICK_401(server, user, target));
            return;
        }

        var targetUsers = server.GetUsers()
            .Where(targetUser => NicknameMatches(targetUser, targetUserInChannel.GetAddress().Nickname))
            .ToList();

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