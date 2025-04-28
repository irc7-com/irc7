using System.Text.RegularExpressions;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Irc.Objects.User;

namespace Irc.Commands;

public class Who : Command, ICommand
{
    public Who() : base(1)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Data;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = chatFrame.Server;
        var user = chatFrame.User;
        var userIsOperator = user.GetLevel() >= EnumUserAccessLevel.Guide;
        var criteria = chatFrame.ChatMessage.Parameters.First();
        var operOnly = false;

        if (chatFrame.ChatMessage.Parameters.Count > 1)
        {
            if (chatFrame.ChatMessage.Parameters[1] == "o")
            {
                operOnly = true;
            }
        }

        if (criteria == "0")
        {
            SendWho(server, user, server.GetUsers(), userIsOperator, operOnly);
        }
        else if (Channel.ValidName(criteria))
        {
            var channel = server.GetChannelByName(criteria);
            if (channel == null)
            {
                user.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(server, user, criteria));
                return;
            }

            var userIsOnChannel = user.IsOn(channel);
            var canIgnoreInvisible = userIsOnChannel || userIsOperator;

            if (user.IsOn(channel) || (!channel.Modes.Secret.ModeValue && !channel.Modes.Private.ModeValue) || userIsOperator)
                SendWho(server, user, channel.GetMembers().Select(m => m.GetUser()).ToList(),
                    canIgnoreInvisible, operOnly);
        }
        else
        {
            var matchedUsers = new List<IUser>();
            foreach (var matchUser in server.GetUsers())
            {
                if (Tools.MatchesMask(matchUser.GetAddress().Nickname, criteria)) matchedUsers.Add(matchUser);
            }

            SendWho(server, user, matchedUsers, userIsOperator, operOnly);
        }

        // 315     RPL_ENDOFWHO
        //                 "<name> :End of /WHO list"
        user.Send(Raws.IRCX_RPL_ENDOFWHO_315(server, user, criteria));
    }

    // TODO: The below function needs re-writing
    public static void SendWho(IServer server, IUser user, IList<IUser> chatUsers,
        bool ignoreInvisible, bool operOnly = false)
    {
        foreach (var chatUser in chatUsers)
        {
            if (operOnly)
            {
                if (user.GetLevel() < EnumUserAccessLevel.Guide)
                {
                    continue;
                }
            }
            
            var isCurrentUser = user == chatUser;
            var userModes = (UserModes)chatUser.Modes;
            if (!userModes.Invisible.ModeValue || ignoreInvisible || isCurrentUser)
            {
                // 352     RPL_WHOREPLY
                //                 "<channel> <user> <host> <server> <nick> \
                //                  <H|G>[*][@|+] :<hopcount> <real name>"

                var address = chatUser.GetAddress();
                var channels = chatUser.GetChannels();
                var channel = channels.Count > 0 ? channels.First().Key : null;
                var channelStoredName = channels.Count > 0 ? channel?.GetName() ?? string.Empty : string.Empty;
                var goneHome = chatUser.Away ? "G" : "H";

                var chanMode = string.Empty;
                var channelMember = channel?.GetMember(user);
                if (channelMember != null) chanMode = channel?.GetMember(user)?.GetModeString();

                var modeString = chatUser.Modes.ToString();

                user.Send(Raws.IRCX_RPL_WHOREPLY_352(
                    server,
                    user,
                    channelStoredName,
                    address.User,
                    address.Host,
                    chatUser.Server.Name,
                    chatUser.Name,
                    $"{goneHome}{chanMode}{modeString}",
                    0,
                    address.RealName
                ));
            }
        }
    }
}