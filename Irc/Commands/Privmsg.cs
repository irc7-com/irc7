using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Irc.Objects.User;

namespace Irc.Commands;

public class Privmsg : Command, ICommand
{
    public new void Execute(IChatFrame chatFrame)
    {
        SendMessage(chatFrame, false);
    }

    // TODO: Refactor this as it duplicates Privmsg
    public static void SendMessage(IChatFrame chatFrame, bool notice)
    {
        var targetName = chatFrame.ChatMessage.Parameters.First();
        var message = chatFrame.ChatMessage.Parameters[1];

        var targets = targetName.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var target in targets)
        {
            IChatObject? chatObject = null;
            if (Channel.ValidName(target))
                chatObject = (IChatObject?)chatFrame.Server.GetChannelByName(target);
            else
                chatObject = (IChatObject?)chatFrame.Server.GetUserByNickname(target);

            if (chatObject == null)
            {
                // TODO: To make common function for this
                chatFrame.User.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User, target));
                return;
            }

            if (chatObject is Channel)
            {
                var user = (User)chatFrame.User;
                var channel = (Channel)chatObject;
                var channelModes = channel.GetModes();
                var channelMember = channel.GetMember(chatFrame.User);
                var isOnChannel = channelMember != null;
                var noExtern = channelModes.NoExtern;
                var moderated = channelModes.Moderated;
                var subscriberOnly = channelModes.Subscriber;

                // Cannot send as a non-subscriber
                if (user.GetLevel() < EnumUserAccessLevel.Guide &&
                    !user.GetProfile().IsSubscriber &&
                    subscriberOnly
                   )
                {
                    chatFrame.User.Send(
                        Raws.IRCX_ERR_CANNOTSENDTOCHAN_404(chatFrame.Server, chatFrame.User, channel));
                    return;
                }

                if (
                    // No External Messages
                    (!isOnChannel && noExtern) ||
                    // Moderated
                    (isOnChannel && moderated && channelMember!.IsNormal())
                )
                {
                    chatFrame.User.Send(
                        Raws.IRCX_ERR_CANNOTSENDTOCHAN_404(chatFrame.Server, chatFrame.User, channel));
                    return;
                }

                if (notice) ((Channel)chatObject).SendNotice(chatFrame.User, message);
                else ((Channel)chatObject).SendMessage(chatFrame.User, message);
            }
            else if (chatObject is User)
            {
                if (notice)
                    ((User)chatObject).Send(
                        Raws.RPL_NOTICE_USER(chatFrame.Server, chatFrame.User, chatObject, message)
                    );
                else
                    ((User)chatObject).Send(
                        Raws.RPL_PRIVMSG_USER(chatFrame.Server, chatFrame.User, chatObject, message)
                    );
            }
        }
    }
}