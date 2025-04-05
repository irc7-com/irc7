using Irc.Commands;
using Irc.Enumerations;
using Irc.Extensions.Commands;

namespace Irc.Extensions.Protocols;

public class IrcX : Irc.Protocols.Irc
{
    public IrcX(): base()
    {
        AddCommand(new Commands.Access());
        AddCommand(new Away());
        AddCommand(new Create());
        AddCommand(new Data());
        AddCommand(new Event());
        AddCommand(new Isircx());
        AddCommand(new Kill());
        AddCommand(new Listx());
        AddCommand(new Reply());
        AddCommand(new Request());
        AddCommand(new Whisper());

    }

    public override EnumProtocolType GetProtocolType()
    {
        return EnumProtocolType.IRCX;
    }
}