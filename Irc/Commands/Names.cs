using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

internal class Names : Command, ICommand
{
    public Names() : base(1)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var user = chatFrame.User;
        var channelNames = chatFrame.ChatMessage.Parameters.First()
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var channelName in channelNames)
        {
            var channel = chatFrame.Server.GetChannelByName(channelName.Trim());

            if (channel != null)
            {
                if (user.IsOn(channel) || (!channel.Modes.Private && !channel.Modes.Secret))
                    ProcessNamesReply(user, channel);
            }
            else
            {
                chatFrame.User.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User, channelName));
            }
        }
    }

    public static void ProcessNamesReply(IUser user, IChannel channel)
    {
        // RFC 2812 "=" for others(public channels).
        var channelType = '=';

        if (channel.Modes.Secret)
            // RFC 2812 "@" is used for secret channels
            channelType = '@';
        else if (channel.Modes.Private)
            // RFC 2812 "*" for private
            channelType = '*';

        user.Send(
            Raws.IRCX_RPL_NAMEREPLY_353(user.Server, user, channel, channelType,
                string.Join(' ',
                    channel.GetMembers().Select(m =>
                        $"{user.GetProtocol().FormattedUser(m)}"
                    )
                )
            )
        );
        user.Send(Raws.IRCX_RPL_ENDOFNAMES_366(user.Server, user, channel));
    }
}