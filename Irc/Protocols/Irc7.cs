﻿using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.User;

namespace Irc.Protocols;

internal class Irc7 : Irc6
{
    public override string FormattedUser(IChannelMember member)
    {
        var modeChar = string.Empty;
        if (member.HasModes()) modeChar += member.Owner.ModeValue ? '.' : member.Operator.ModeValue ? '@' : '+';

        var profile = ((User)member.GetUser()).GetProfile().Irc7_ToString();
        return $"{profile},{modeChar}{member.GetUser().GetAddress().Nickname}";
    }

    public override EnumProtocolType GetProtocolType()
    {
        return EnumProtocolType.IRC7;
    }

    public override string GetFormat(IUser user)
    {
        return ((User)user).GetProfile().Irc7_ToString();
    }
}