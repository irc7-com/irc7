using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;
using Irc.Objects.Channel;

public class Listx : Command, ICommand
{
    public Listx() : base()
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
    var server = chatFrame.Server;
    var user = chatFrame.User;
    var parameters = chatFrame.ChatMessage.Parameters;
    var channels = server.GetChannels();
    var firstParam = parameters.FirstOrDefault();

    if (firstParam != null)
    {
        var queryTerms = Tools.CSVToArray(firstParam);
        foreach (var term in queryTerms)
        {
            if (term.StartsWith("<") && int.TryParse(term.Substring(1), out var lessThan))
            {
                channels = channels.Where(c => c.GetMembers().Count < lessThan).ToList();
            }
            else if (term.StartsWith(">") && int.TryParse(term.Substring(1), out var greaterThan))
            {
                channels = channels.Where(c => c.GetMembers().Count > greaterThan).ToList();
            }
            else if (term.StartsWith("C<") && int.TryParse(term.Substring(2), out var createdLessThan))
            {
                var epochNow = Resources.GetEpochNowInSeconds();
                channels = channels.Where(c => ((epochNow - c.Creation) / 60) < createdLessThan).ToList();
            }
            else if (term.StartsWith("C>") && int.TryParse(term.Substring(2), out var createdGreaterThan))
            {
                var epochNow = Resources.GetEpochNowInSeconds();
                channels = channels.Where(c => ((epochNow - c.Creation) / 60) > createdGreaterThan).ToList();
            }
            else if (term.StartsWith("L="))
            {
                var mask = term.Substring(2);
                channels = channels.Where(c => Tools.MatchesMask(c.Props.Language.Value, mask)).ToList();
            }
            else if (term.StartsWith("N="))
            {
                var mask = term.Substring(2);
                channels = channels.Where(c => Tools.MatchesMask(c.Name, mask)).ToList();
            }
            else if (term == "R=0")
            {
                channels = channels.Where(c => !c.Modes.Registered.ModeValue).ToList();
            }
            else if (term == "R=1")
            {
                channels = channels.Where(c => c.Modes.Registered.ModeValue).ToList();
            }
            else if (term.StartsWith("S="))
            {
                var mask = term.Substring(2);
                channels = channels.Where(c => Tools.MatchesMask(c.Props.Subject.Value, mask)).ToList();
            }
            else if (term.StartsWith("T<") && int.TryParse(term.Substring(2), out var topicChangedLessThan))
            {
                var epochNow = Resources.GetEpochNowInSeconds();
                channels = channels.Where(c => ((epochNow - c.TopicChanged) / 60) < topicChangedLessThan).ToList();
            }
            else if (term.StartsWith("T>") && int.TryParse(term.Substring(2), out var topicChangedGreaterThan))
            {
                var epochNow = Resources.GetEpochNowInSeconds();
                channels = channels.Where(c => ((epochNow - c.TopicChanged) / 60) > topicChangedGreaterThan).ToList();
            }
            else if (term.StartsWith("T="))
            {
                var mask = term.Substring(2);
                channels = channels.Where(c => Tools.MatchesMask(c.Props.Topic.Value, mask)).ToList();
            }
            else if (int.TryParse(term, out var queryLimit))
            {
                channels = channels.Take(queryLimit).ToList();
            }
        }
    }
    else
    {
        channels = server.GetChannels();
    }

    ListChannels(server, user, channels);
    }

    public static void ListChannels(IServer server, IUser user, IList<IChannel> channels)
    {
        // Case "811"      ' Start of LISTX
        user.Send(Raws.IRCX_RPL_LISTXSTART_811(server, user));
        foreach (var channel in channels)
            if (user.IsOn(channel) ||
                user.GetLevel() >= EnumUserAccessLevel.Guide ||
                (!channel.Modes.Secret.ModeValue && !channel.Modes.Private.ModeValue))
                //  :TK2CHATCHATA04 812 'Admin_Koach %#Roomname +tnfSl 0 50 :%Chatroom\c\bFor\bBL\bGames\c\bFun\band\bEvents.
                user.Send(Raws.IRCX_RPL_LISTXLIST_812(
                    server,
                    user,
                    channel,
                    channel.Modes.GetModeString(),
                    channel.GetMembers().Count,
                    channel.Modes.UserLimit.Value,
                    channel.Props.Topic.Value
                ));
        user.Send(Raws.IRCX_RPL_LISTXEND_817(server, user));
    }
}