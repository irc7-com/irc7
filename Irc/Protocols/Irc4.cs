using Irc.Enumerations;

namespace Irc.Protocols;

internal class Irc4 : Irc3
{
    public override EnumProtocolType GetProtocolType()
    {
        return EnumProtocolType.IRC4;
    }
}