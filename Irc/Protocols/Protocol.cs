using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc;

public class Protocol : IProtocol
{
    protected Dictionary<string, ICommand> Commands = new(StringComparer.InvariantCultureIgnoreCase);

    public ICommand GetCommand(string name)
    {
        throw new NotImplementedException();
    }

    public Dictionary<string, ICommand> GetCommands()
    {
        return Commands;
    }

    public EnumProtocolType GetProtocolType()
    {
        throw new NotImplementedException();
    }

    public void AddCommand(ICommand command, string name)
    {
        if (!Commands.ContainsKey(!string.IsNullOrWhiteSpace(name) ? command.GetName() : name))
            Commands.Add(name ?? command.GetName(), command);
    }
    public void AddCommand(ICommand command)
    {
        if (!Commands.ContainsKey(command.GetName())) Commands.Add(command.GetName(), command);
    }

    public void FlushCommands()
    {
        Commands.Clear();
    }

    public virtual string FormattedUser(IChannelMember member)
    {
        var modeChar = string.Empty;
        if (!member.IsNormal()) modeChar += member.IsOwner() ? '.' : member.IsHost() ? '@' : '+';
        return $"{modeChar}{member.GetUser().GetAddress().Nickname}";
    }

    public virtual string GetFormat(IUser user)
    {
        throw new NotImplementedException();
    }

    public void UpdateCommand(ICommand command)
    {
        var commandName = command.GetName();
        if (Commands.ContainsKey(commandName))
            Commands[commandName] = command;
    }

    public void UpdateCommand(ICommand command, string name)
    {
        var commandName = !string.IsNullOrWhiteSpace(name) ? name : command.GetName();
        if (Commands.ContainsKey(commandName))
            Commands[commandName] = command;
    }
}