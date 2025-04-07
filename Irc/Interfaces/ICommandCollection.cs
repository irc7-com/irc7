namespace Irc.Interfaces;

public interface ICommandCollection
{
    ICommand GetCommand(string Name);
}