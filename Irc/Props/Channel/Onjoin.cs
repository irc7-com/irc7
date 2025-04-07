﻿using Irc.Constants;

namespace Irc.Props.Channel;

internal class Onjoin : PropRule
{
    // The ONJOIN channel property contains a string to be sent (via PRIVMSG) to a user after the user has joined the channel.
    // The channel name is displayed as the sender of the message.
    // Only the user joining the channel will see this message.
    // Multiple lines can be generated by embedding '\n' in the string.
    // The ONJOIN property is limited to 255 characters.
    public Onjoin() : base(ExtendedResources.ChannelPropOnJoin, EnumChannelAccessLevel.ChatHost,
        EnumChannelAccessLevel.ChatHost, Resources.ChannelPropOnjoinRegex, string.Empty)
    {
    }
}