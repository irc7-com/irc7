using System.Text;
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
                if (user.IsOn(channel) || (!channel.Modes.Private.ModeValue && !channel.Modes.Secret.ModeValue))
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
        // RFC 2812: '@' secret, '*' private, '=' all others (public).
        var channelType = channel.Modes.Secret.ModeValue ? '@'
                        : channel.Modes.Private.ModeValue ? '*'
                        : '=';

        // Measure available bytes for names: MaxMessageLength minus CRLF (2) minus the fixed prefix.
        var prefix = $":{user.Server} 353 {user} {channelType} {channel} :";
        var maxNamesLength = user.Server.MaxMessageLength - 2 - prefix.Length;

        var batch = new StringBuilder();
        foreach (var name in channel.GetMembers().Select(m => user.GetProtocol().FormattedUser(m)))
        {
            if (batch.Length > 0 && batch.Length + 1 + name.Length > maxNamesLength)
            {
                user.Send(Raws.IRCX_RPL_NAMEREPLY_353(user.Server, user, channel, channelType, batch.ToString()));
                batch.Clear();
            }

            if (batch.Length > 0) batch.Append(' ');
            batch.Append(name);
        }

        if (batch.Length > 0)
            user.Send(Raws.IRCX_RPL_NAMEREPLY_353(user.Server, user, channel, channelType, batch.ToString()));

        user.Send(Raws.IRCX_RPL_ENDOFNAMES_366(user.Server, user, channel));
    }
}