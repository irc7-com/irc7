using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;

namespace Irc.Commands;

public class Event : Command, ICommand
{
    public Event() : base(1)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var parameters = chatFrame.ChatMessage.Parameters;
        var operation = parameters[0].ToUpperInvariant();

        switch (operation)
        {
            case "LIST":
                ListQuestions(chatFrame, parameters);
                break;
            case "ADD":
                AddQuestion(chatFrame, parameters);
                break;
            case "DELETE":
                DeleteQuestion(chatFrame, parameters);
                break;
            default:
                chatFrame.User.Send(
                    Raws.IRCX_ERR_NOSUCHEVENT_920(chatFrame.Server, chatFrame.User, operation));
                break;
        }
    }

    public static void SendQuestionAdded(IServer server, IChannel channel, OnStageQuestion question)
    {
        foreach (var member in channel.GetMembers().Where(Channel.IsOnStageHost))
        {
            member.GetUser().Send(
                Raws.IRCX_RPL_EVENTADD_806(server, member.GetUser(), channel, question.ToEventData()));
        }
    }

    private static void SendQuestionDeleted(IServer server, IChannel channel, OnStageQuestion question)
    {
        foreach (var member in channel.GetMembers().Where(Channel.IsOnStageHost))
        {
            member.GetUser().Send(
                Raws.IRCX_RPL_EVENTDEL_807(server, member.GetUser(), channel, question.ToEventData()));
        }
    }

    private static void ListQuestions(IChatFrame chatFrame, List<string> parameters)
    {
        if (!TryGetManagedChannel(chatFrame, parameters.Count > 1 ? parameters[1] : null, out var channel))
        {
            return;
        }

        chatFrame.User.Send(Raws.IRCX_RPL_EVENTSTART_808(chatFrame.Server, chatFrame.User, channel));
        foreach (var question in channel.GetOnStageQuestions())
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_EVENTLIST_809(chatFrame.Server, chatFrame.User, channel, question.ToEventData()));
        }
        chatFrame.User.Send(Raws.IRCX_RPL_EVENTEND_810(chatFrame.Server, chatFrame.User, channel));
    }

    private static void AddQuestion(IChatFrame chatFrame, List<string> parameters)
    {
        if (parameters.Count < 5)
        {
            chatFrame.User.Send(
                Raws.IRCX_ERR_NEEDMOREPARAMS_461(chatFrame.Server, chatFrame.User, nameof(Event)));
            return;
        }

        if (!TryGetManagedChannel(chatFrame, parameters[1], out var channel))
        {
            return;
        }

        var nick = parameters[2];
        var sourceRoom = parameters[3];
        var message = parameters[4];
        var question = channel.AddOnStageQuestion(nick, sourceRoom, message);
        SendQuestionAdded(chatFrame.Server, channel, question);
    }

    private static void DeleteQuestion(IChatFrame chatFrame, List<string> parameters)
    {
        if (parameters.Count < 3)
        {
            chatFrame.User.Send(
                Raws.IRCX_ERR_NEEDMOREPARAMS_461(chatFrame.Server, chatFrame.User, nameof(Event)));
            return;
        }

        if (!TryGetManagedChannel(chatFrame, parameters[1], out var channel))
        {
            return;
        }

        if (!int.TryParse(parameters[2], out var id))
        {
            chatFrame.User.Send(
                Raws.IRCX_ERR_EVENTMIS_919(chatFrame.Server, chatFrame.User, channel, parameters[2]));
            return;
        }

        var question = channel.GetOnStageQuestions().FirstOrDefault(q => q.Id == id);
        if (question == null || !channel.RemoveOnStageQuestion(id))
        {
            chatFrame.User.Send(
                Raws.IRCX_ERR_EVENTMIS_919(chatFrame.Server, chatFrame.User, channel, parameters[2]));
            return;
        }

        SendQuestionDeleted(chatFrame.Server, channel, question);
    }

    private static bool TryGetManagedChannel(
        IChatFrame chatFrame,
        string? channelName,
        out IChannel channel)
    {
        channel = null!;
        var targetName = channelName;
        if (string.IsNullOrWhiteSpace(targetName))
        {
            targetName = chatFrame.User.GetChannels().Keys.Count == 1
                ? chatFrame.User.GetChannels().Keys.First().GetName()
                : string.Empty;
        }

        if (string.IsNullOrWhiteSpace(targetName) || !Channel.ValidName(targetName))
        {
            chatFrame.User.Send(
                Raws.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User, targetName));
            return false;
        }

        channel = chatFrame.Server.GetChannelByName(targetName)!;
        if (channel == null)
        {
            chatFrame.User.Send(
                Raws.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User, targetName));
            return false;
        }

        var channelMember = channel.GetMember(chatFrame.User);
        if (channelMember == null)
        {
            chatFrame.User.Send(
                Raws.IRCX_ERR_NOTONCHANNEL_442(chatFrame.Server, chatFrame.User, channel));
            return false;
        }

        if (!channel.Modes.OnStage.ModeValue)
        {
            chatFrame.User.Send(
                Raws.IRCX_ERR_CANNOTSENDTOCHAN_404(chatFrame.Server, chatFrame.User, channel));
            return false;
        }

        if (!Channel.IsOnStageHost(channelMember))
        {
            chatFrame.User.Send(
                Raws.IRCX_ERR_CHANOPRIVSNEEDED_482(chatFrame.Server, chatFrame.User, channel));
            return false;
        }

        return true;
    }
}
