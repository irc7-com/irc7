using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;

namespace Irc.Commands;

public class Esubmit : Command, ICommand
{
    public Esubmit() : base(2)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    // ESUBMIT %#OnStage :Why am I here?
    public new void Execute(IChatFrame chatFrame)
    {
        var targetName = chatFrame.ChatMessage.Parameters.First();
        var message = chatFrame.ChatMessage.Parameters[1];

        var targets = targetName.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var target in targets)
        {
            // TODO: Consider combining the below two blocks
            if (!Channel.ValidName(target))
            {
                chatFrame.User.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User, target));
                return;
            }

            var chatObject = (IChatObject?)chatFrame.Server.GetChannelByName(target);
            var channel = (IChannel?)chatObject;
            if (channel == null)
            {
                chatFrame.User.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User, target));
                return;
            }

            if (!channel!.Modes.OnStage.ModeValue)
            {
                chatFrame.User.Send(
                    Raws.IRCX_ERR_CANNOTSENDTOCHAN_404(chatFrame.Server, chatFrame.User, channel));
                return;
            }

            var sourceRoom = GetSourceRoom(chatFrame.User, channel);
            if (string.IsNullOrWhiteSpace(sourceRoom))
            {
                chatFrame.User.Send(
                    Raws.IRCX_ERR_NOTONCHANNEL_442(chatFrame.Server, chatFrame.User, channel));
                return;
            }

            SubmitQuestion(chatFrame.Server, chatFrame.User, channel, message, sourceRoom);
        }
    }

    public static OnStageQuestion SubmitQuestion(
        IServer server,
        IUser user,
        IChannel channel,
        string message,
        string? sourceRoom = null)
    {
        var question = channel.AddOnStageQuestion(user, message, sourceRoom ?? channel.GetName());
        Event.SendQuestionAdded(server, channel, question);
        return question;
    }

    private static string? GetSourceRoom(IUser user, IChannel targetChannel)
    {
        if (targetChannel.GetMember(user) != null)
        {
            return targetChannel.GetName();
        }

        return user.GetChannels().Keys.Count == 1
            ? user.GetChannels().Keys.First().GetName()
            : null;
    }
}
