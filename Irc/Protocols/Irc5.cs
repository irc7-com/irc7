using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.User;

namespace Irc.Protocols;

internal class Irc5 : Irc4
{
    public override string FormattedUser(IChannelMember member)
    {
        var modeChar = string.Empty;
        if (!member.IsNormal()) modeChar += member.IsOwner() ? '.' : member.IsHost() ? '@' : '+';

        var profile = ((User)member.GetUser()).GetProfile().Irc5_ToString();
        return $"{profile},{modeChar}{member.GetUser().GetAddress().Nickname}";
    }

    public override EnumProtocolType GetProtocolType()
    {
        return EnumProtocolType.IRC5;
    }

    public override string GetFormat(IUser user)
    {
        return ((User)user).GetProfile().Irc5_ToString();
    }
}