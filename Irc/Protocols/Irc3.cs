using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Protocols;

public class Irc3 : IrcX
{
    public Irc3()
    {
        AddCommand(new Goto());
        AddCommand(new Esubmit());
        AddCommand(new Eprivmsg());
        AddCommand(new Equestion());
        UpdateCommand(new Privmsg());
        UpdateCommand(new Notice());
    }

    public override EnumProtocolType GetProtocolType()
    {
        return EnumProtocolType.IRC3;
    }

    public override string FormattedUser(IChannelMember member)
    {
        var modeChar = string.Empty;
        if (!member.IsNormal()) modeChar += member.IsOwner() ? '.' : member.IsHost() ? '@' : '+';
        return $"{modeChar}{member.GetUser().GetAddress().Nickname}";
    }
}