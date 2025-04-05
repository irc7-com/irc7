using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;
using Irc.Objects.Channel;
using Irc.Objects.User;

namespace Irc.Commands;

public class Privmsg : Command, ICommand
{
    public Privmsg() : base(2)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Standard;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        SendMessage(chatFrame, false);
    }

    public static void SendMessage(IChatFrame chatFrame, bool Notice)
    {
        var targetName = chatFrame.Message.Parameters.First();
        var message = chatFrame.Message.Parameters[1];

        var targets = targetName.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var target in targets)
        {
            IChatObject chatObject = null;
            if (Channel.ValidName(target))
                chatObject = (IChatObject)chatFrame.Server.GetChannelByName(target);
            else
                chatObject = (IChatObject)chatFrame.Server.GetUserByNickname(target);

            if (chatObject == null)
            {
                // TODO: To make common function for this
                chatFrame.User.Send(Raw.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User, target));
                return;
            }

            if (chatObject is Channel)
            {
                var channel = (IChannel)chatObject;
                var channelMember = channel.GetMember(chatFrame.User);
                var isOnChannel = channelMember != null;
                var noExtern = channel.Modes.NoExtern;
                var moderated = channel.Modes.Moderated;

                if (
                    // No External Messages
                    (!isOnChannel && noExtern) ||
                    // Moderated
                    (isOnChannel && moderated && channelMember.IsNormal())
                )
                {
                    chatFrame.User.Send(
                        Raw.IRCX_ERR_CANNOTSENDTOCHAN_404(chatFrame.Server, chatFrame.User, channel));
                    return;
                }

                if (Notice) ((Channel)chatObject).SendNotice(chatFrame.User, message);
                else ((Channel)chatObject).SendMessage(chatFrame.User, message);
            }
            else if (chatObject is User)
            {
                if (Notice)
                    ((User)chatObject).Send(
                        Raw.RPL_NOTICE_USER(chatFrame.Server, chatFrame.User, chatObject, message)
                    );
                else
                    ((User)chatObject).Send(
                        Raw.RPL_PRIVMSG_USER(chatFrame.Server, chatFrame.User, chatObject, message)
                    );
            }
        }
    }
}