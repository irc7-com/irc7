using Irc.Commands;
using Irc.Enumerations;

public class IrcX : Irc.Protocols.Irc
{
    public override EnumProtocolType GetProtocolType()
    {
        return EnumProtocolType.IRCX;
    }
}