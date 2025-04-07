using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;

internal class Ircvers : Command, ICommand
{
    public Ircvers() : base(2, false)
    {
    }

    public EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Standard;
    }

    public void Execute(IChatFrame chatFrame)
    {
    }
}