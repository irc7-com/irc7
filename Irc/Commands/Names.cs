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
        // RFC 2812 "=" for others(public channels).
        var channelType = '=';

        if (channel.Modes.Secret.ModeValue)
            // RFC 2812 "@" is used for secret channels
            channelType = '@';
        else if (channel.Modes.Private.ModeValue)
            // RFC 2812 "*" for private
            channelType = '*';

        // Calculate the fixed prefix length for a 353 reply:
        // :{server} 353 {user} {channelType} {channel} :
        var prefixLength = 1 + user.Server.ToString().Length + 5 + user.ToString().Length
                           + 1 + 1 + 1 + channel.ToString().Length + 2;

        // Reserve 2 bytes for CRLF when determining how many name bytes fit per message.
        var maxNamesLength = user.Server.MaxMessageLength - 2 - prefixLength;

        var formattedNames = channel.GetMembers()
            .Select(m => user.GetProtocol().FormattedUser(m))
            .ToList();

        var currentNames = new List<string>();
        var currentLength = 0;

        foreach (var name in formattedNames)
        {
            // A space separator is needed before every name except the first in a batch.
            var spaceNeeded = currentNames.Count > 0 ? 1 : 0;

            if (currentNames.Count > 0 && currentLength + spaceNeeded + name.Length > maxNamesLength)
            {
                user.Send(Raws.IRCX_RPL_NAMEREPLY_353(user.Server, user, channel, channelType,
                    string.Join(' ', currentNames)));
                currentNames.Clear();
                currentLength = 0;
                spaceNeeded = 0;
            }

            currentNames.Add(name);
            currentLength += spaceNeeded + name.Length;
        }

        if (currentNames.Count > 0)
            user.Send(Raws.IRCX_RPL_NAMEREPLY_353(user.Server, user, channel, channelType,
                string.Join(' ', currentNames)));

        user.Send(Raws.IRCX_RPL_ENDOFNAMES_366(user.Server, user, channel));
    }
}