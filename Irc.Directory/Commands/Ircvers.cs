using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Directory.Commands;

internal class Ircvers : Command, ICommand
{
    public Ircvers() : base(2, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Standard;
    }

    public new void Execute(IChatFrame chatFrame)
    {
    }
}