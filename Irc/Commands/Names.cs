using System.Text;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

internal class Names : Command, ICommand
{
    public Names() : base(0)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var user = chatFrame.User;
        var server = chatFrame.Server;
        var parameters = chatFrame.ChatMessage.Parameters;

        if (parameters.Count == 0)
        {
            var visibleChannels = server.GetChannels()
                .Where(channel => user.IsOn(channel) || (!channel.Modes.Private.ModeValue && !channel.Modes.Secret.ModeValue))
                .ToList();

            visibleChannels.ForEach(channel => ProcessNamesReply(user, channel, false));
            user.Send(Raws.IRCX_RPL_ENDOFNAMES_366(server, user, "*"));
            return;
        }

        var channelNames = parameters.First()
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var channelName in channelNames)
        {
            var trimmedChannelName = channelName.Trim();
            var channel = server.GetChannelByName(trimmedChannelName);

            if (channel != null)
            {
                if (user.IsOn(channel) || (!channel.Modes.Private.ModeValue && !channel.Modes.Secret.ModeValue))
                {
                    ProcessNamesReply(user, channel);
                    continue;
                }
            }

            user.Send(Raws.IRCX_RPL_ENDOFNAMES_366(server, user, trimmedChannelName));
        }
    }

    public static void ProcessNamesReply(IUser user, IChannel channel, bool sendEndOfNames = true)
    {
        // RFC 2812 "=" for others(public channels).
        var channelType = '=';

        if (channel.Modes.Secret.ModeValue)
            // RFC 2812 "@" is used for secret channels
            channelType = '@';
        else if (channel.Modes.Private.ModeValue)
            // RFC 2812 "*" for private
            channelType = '*';

        var members = channel.GetMembers();
        var namesBuilder = new StringBuilder();
        for (var i = 0; i < members.Count; i++)
        {
            if (i > 0)
            {
                namesBuilder.Append(' ');
            }

            namesBuilder.Append(user.GetProtocol().FormattedUser(members[i]));
        }

        user.Send(Raws.IRCX_RPL_NAMEREPLY_353(user.Server, user, channel, channelType, namesBuilder.ToString()));

        if (sendEndOfNames)
        {
            user.Send(Raws.IRCX_RPL_ENDOFNAMES_366(user.Server, user, channel));
        }
    }
}