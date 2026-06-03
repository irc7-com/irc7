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

        // ERR_NOSUCHNICK: search server-wide, not just in the caller's first channel
        var targetUser = server.GetUserByNickname(target);
        if (targetUser == null)
        {
            user.Send(Raws.IRCX_ERR_NOSUCHNICK_401(server, user, target));
            return;
        }

        // Prevent killing a user with equal or higher privileges
        if (targetUser.GetLevel() >= user.GetLevel())
        {
            user.Send(Raws.IRCX_ERR_SECURITY_908(server, user));
            return;
        }

        // Remove the target from every channel they are in, then disconnect
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