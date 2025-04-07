using Irc;
using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;

public class Eprivmsg : Command, ICommand
{
    public Eprivmsg() : base(2)
    {
    }

    public EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    // EPRIVMSG %#OnStage :Why am I here?
    public void Execute(IChatFrame chatFrame)
    {
        var targetName = chatFrame.Message.Parameters.First();
        var message = chatFrame.Message.Parameters[1];

        var targets = targetName.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var target in targets)
        {
            // TODO: Below two blocks need combining
            if (!Channel.ValidName(target))
            {
                chatFrame.User.Send(Raw.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User, target));
                return;
            }

            var chatObject = (IChatObject?)chatFrame.Server.GetChannelByName(target);
            if (chatObject == null)
            {
                chatFrame.User.Send(Raw.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User, target));
                return;
            }

            if (chatObject is IChannel channel)
            {
                var channelMember = channel.GetMember(chatFrame.User);
                var isOnChannel = channelMember != null;

                if (!isOnChannel)
                {
                    chatFrame.User.Send(
                        Raw.IRCX_ERR_NOTONCHANNEL_442(chatFrame.Server, chatFrame.User, channel));
                    return;
                }

                if (!((IApolloChannelModes)channel.Modes).OnStage)
                {
                    chatFrame.User.Send(
                        Raw.IRCX_ERR_CANNOTSENDTOCHAN_404(chatFrame.Server, chatFrame.User, channel));
                    return;
                }

                SendEprivmsg(chatFrame.User, channel, message);
            }
        }
    }

    public static void SendEprivmsg(IUser user, IChannel channel, string message)
    {
        channel.Send(ApolloRaws.RPL_EPRIVMSG(user, channel, message));
    }
}