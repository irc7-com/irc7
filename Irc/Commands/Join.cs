using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;

namespace Irc.Commands;

public class Join : Command, ICommand
{
    public Join() : base(1)
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
        var channels = chatFrame.Message.Parameters.First();
        var key = chatFrame.Message.Parameters.Count > 1 ? chatFrame.Message.Parameters[1] : string.Empty;

        var channelNames = ValidateChannels(server, user, channels);
        if (channelNames.Count == 0) return;

        JoinChannels(server, user, channelNames, key);
    }

    public static List<string> ValidateChannels(IServer server, IUser user, string channels)
    {
        var channelNames = channels.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (channelNames.Count == 0)
        {
            user.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(server, user, string.Empty));
        }
        else
        {
            var invalidChannelNames = channelNames.Where(c => !Channel.ValidName(c)).ToList();
            channelNames.RemoveAll(c => invalidChannelNames.Contains(c));

            // TODO: Could do better below for reporting invalid channel / empty channel
            if (invalidChannelNames.Count > 0)
                invalidChannelNames.ForEach(c => user.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(server, user, c)));
        }

        return channelNames;
    }

    public static void JoinChannels(IServer server, IUser user, List<string> channelNames, string key)
    {
        // TODO: Optimize the below code
        foreach (var channelName in channelNames)
        {
            if (user.GetChannels().Count >= server.MaxChannels)
            {
                user.Send(Raws.IRCX_ERR_TOOMANYCHANNELS_405(server, user, channelName));
                continue;
            }

            var channel = server
                .GetChannelByName(channelName) ?? server.CreateChannel(user, channelName, key);

            if (channel.HasUser(user))
            {
                user.Send(Raws.IRCX_ERR_ALREADYONCHANNEL_927(server, user, channel));
                continue;
            }

            var channelAccessResult = channel.GetAccess(user, key);
            if (channelAccessResult < EnumChannelAccessResult.SUCCESS_GUEST)
            {
                SendJoinError(server, channel, user, channelAccessResult);
                continue;
            }

            channel.Join(user, channelAccessResult)
                .SendTopic(user)
                .SendNames(user);
        }
    }

    public static void SendJoinError(IServer server, IChannel channel, IUser user, EnumChannelAccessResult result)
    {
        switch (result)
        {
            case EnumChannelAccessResult.ERR_BADCHANNELKEY:
            {
                user.Send(Raws.IRCX_ERR_BADCHANNELKEY_475(server, user, channel));
                break;
            }
            case EnumChannelAccessResult.ERR_INVITEONLYCHAN:
            {
                user.Send(Raws.IRCX_ERR_INVITEONLYCHAN_473(server, user, channel));
                break;
            }
            case EnumChannelAccessResult.ERR_CHANNELISFULL:
            {
                user.Send(Raws.IRCX_ERR_CHANNELISFULL_471(server, user, channel));
                break;
            }
            default:
            {
                user.Send($"CANNOT JOIN CHANNEL {result.ToString()}");
                break;
            }
        }
    }
}