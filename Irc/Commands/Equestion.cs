using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;

namespace Irc.Commands;

public class Equestion : Command, ICommand
{
    public Equestion() : base(3)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    // EQUESTION %#OnStage Nickname :Why am I here?
    public new void Execute(IChatFrame chatFrame)
    {
        var parameters = chatFrame.ChatMessage.Parameters;
        var targetName = parameters.First();
        var nickname = parameters[1];
        var hasSourceRoom = parameters.Count >= 4;
        var sourceRoomParam = hasSourceRoom ? parameters[2] : string.Empty;
        var message = hasSourceRoom ? parameters[3] : parameters[2];

        var targets = targetName.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var target in targets)
        {
            // TODO: Below two blocks need combining
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

            var channelMember = channel?.GetMember(chatFrame.User);
            var isOnChannel = channelMember != null;

            if (!isOnChannel)
            {
                chatFrame.User.Send(
                    Raws.IRCX_ERR_NOTONCHANNEL_442(chatFrame.Server, chatFrame.User, channel!));
                return;
            }

            if (!channel!.Modes.OnStage.ModeValue)
            {
                chatFrame.User.Send(
                    Raws.IRCX_ERR_CANNOTSENDTOCHAN_404(chatFrame.Server, chatFrame.User, channel));
                return;
            }

            if (!Channel.IsOnStageHost(channelMember))
            {
                chatFrame.User.Send(
                    Raws.IRCX_ERR_CHANOPRIVSNEEDED_482(chatFrame.Server, chatFrame.User, channel));
                return;
            }

            var sourceRoom = hasSourceRoom ? sourceRoomParam : target;
            SubmitQuestion(chatFrame.User, channel, nickname, sourceRoom, message);
        }
    }

    public static void SubmitQuestion(
        IUser user,
        IChannel channel,
        string nickname,
        string sourceRoom,
        string message)
    {
        channel.Send(Raws.RPL_EQUESTION(user, channel, nickname, sourceRoom, message));
    }
}
