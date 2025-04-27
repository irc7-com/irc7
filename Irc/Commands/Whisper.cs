using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc.Commands;

internal class Whisper : Command, ICommand
{
    public Whisper() : base(1, true, 3)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        // <sender> WHISPER <channel> <nick list> :<text>

        var server = chatFrame.Server;
        var user = chatFrame.User;

        if (chatFrame.ChatMessage.Parameters.Count == 1)
        {
            user.Send(Raws.IRC_ERR_NORECIPIENT_411(server, user, nameof(Whisper)));
            return;
        }

        if (chatFrame.ChatMessage.Parameters.Count == 2)
        {
            user.Send(Raws.IRC_ERR_NOTEXT_412(server, user, nameof(Whisper)));
            return;
        }

        var channelName = chatFrame.ChatMessage.Parameters.First();
        var channel = chatFrame.Server.GetChannelByName(channelName);
        if (channel == null)
        {
            user.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(server, user, channelName));
            return;
        }

        var channelModes = channel.Modes;

        if (!user.IsOn(channel))
        {
            chatFrame.User.Send(
                Raws.IRCX_ERR_NOTONCHANNEL_442(server, user, channel));
            return;
        }

        if (channelModes.NoWhisper.ModeValue)
        {
            user.Send(Raws.IRCX_ERR_NOWHISPER_923(server, user, channel));
            return;
        }

        if (channelModes.NoGuestWhisper.ModeValue && user.IsGuest() && user.GetLevel() < EnumUserAccessLevel.Guide)
        {
            user.Send(Raws.IRCX_ERR_NOWHISPER_923(server, user, channel));
            return;
        }

        var targetNickname = chatFrame.ChatMessage.Parameters[1];
        var target = channel.GetMemberByNickname(targetNickname);
        if (target == null)
        {
            user.Send(Raws.IRCX_ERR_NOSUCHNICK_401(server, user, targetNickname));
            return;
        }

        var message = chatFrame.ChatMessage.Parameters[2];

        if (target.GetUser().GetProtocol().GetProtocolType() < EnumProtocolType.IRCX)
            // PRIVMSG
            target.GetUser().Send(
                Raws.RPL_PRIVMSG_USER(chatFrame.Server, chatFrame.User, (ChatObject)target.GetUser(), message)
            );
        else
            target.GetUser().Send(
                Raws.RPL_CHAN_WHISPER(chatFrame.Server, chatFrame.User, channel, (ChatObject)target.GetUser(), message)
            );
    }
}