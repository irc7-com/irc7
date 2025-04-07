using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc.Commands;

internal class Kick : Command, ICommand
{
    public Kick() : base(2)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Standard;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var source = chatFrame.User;
        var channelName = chatFrame.ChatMessage.Parameters.First();
        var target = chatFrame.ChatMessage.Parameters[1];
        var reason = string.Empty;

        if (chatFrame.ChatMessage.Parameters.Count > 2) reason = chatFrame.ChatMessage.Parameters[2];

        var channel = chatFrame.Server.GetChannelByName(channelName);
        if (channel == null)
        {
            chatFrame.User.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User,
                chatFrame.ChatMessage.Parameters.First()));
        }
        else
        {
            var sourceMember = channel.GetMember(source);
            if (sourceMember == null || !channel.CanBeModifiedBy((ChatObject)source))
            {
                chatFrame.User.Send(Raws.IRCX_ERR_NOTONCHANNEL_442(chatFrame.Server, source, channel));
                return;
            }

            var targetMember = channel.GetMemberByNickname(target);
            if (targetMember == null)
            {
                chatFrame.User.Send(Raws.IRCX_ERR_NOSUCHNICK_401(chatFrame.Server, source, channelName));
                return;
            }

            var result = ProcessKick(channel, sourceMember, targetMember, reason);
            channel.ProcessChannelError(result, chatFrame.Server, sourceMember.GetUser(),
                (ChatObject)targetMember.GetUser(), reason);
        }
    }

    public static EnumIrcError ProcessKick(IChannel channel, IChannelMember source, IChannelMember target,
        string reason)
    {
        var result = channel.CanModifyMember(source, target, EnumChannelAccessLevel.ChatHost);
        if (result != EnumIrcError.OK) return result;

        channel.Kick(source.GetUser(), target.GetUser(), reason);
        return result;
    }
}