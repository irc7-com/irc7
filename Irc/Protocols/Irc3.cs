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
        if (!member.HasModes()) modeChar += member.Owner.ModeValue ? '.' : member.Operator.ModeValue ? '@' : '+';
        return $"{modeChar}{member.GetUser().GetAddress().Nickname}";
    }
}