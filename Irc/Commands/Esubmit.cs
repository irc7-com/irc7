using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;

namespace Irc.Commands;

public class Esubmit : Command, ICommand
{
    public Esubmit() : base(2)
    {
    }

    public EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    // ESUBMIT %#OnStage :Why am I here?
    public void Execute(IChatFrame chatFrame)
    {
        var targetName = chatFrame.Message.Parameters.First();
        var message = chatFrame.Message.Parameters[1];

        var targets = targetName.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var target in targets)
        {
            // TODO: Consider combining the below two blocks
            if (!Channel.ValidName(target))
            {
                chatFrame.User.Send(Raw.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User, target));
                return;
            }

            var chatObject = (IChatObject?)chatFrame.Server.GetChannelByName(target);
            var channel = (IChannel?)chatObject;
            if (channel == null)
            {
                chatFrame.User.Send(Raw.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User, target));
                return;
            }

            var channelMember = channel?.GetMember(chatFrame.User);
            var isOnChannel = channelMember != null;

            if (!isOnChannel)
            {
                chatFrame.User.Send(
                    Raw.IRCX_ERR_NOTONCHANNEL_442(chatFrame.Server, chatFrame.User, channel!));
                return;
            }

            if (!((IApolloChannelModes)channel!.Modes).OnStage)
            {
                chatFrame.User.Send(
                    Raw.IRCX_ERR_CANNOTSENDTOCHAN_404(chatFrame.Server, chatFrame.User, channel));
                return;
            }

            SubmitQuestion(chatFrame.User, channel, message);
        }
    }

    // TODO: Instead of EQUESTION this needs to be something else such as a EVENT etc

    public static void SubmitQuestion(IUser user, IChannel channel, string message)
    {
        channel.Send(ApolloRaws.RPL_EQUESTION(user, channel, user.ToString(), message));
    }
}