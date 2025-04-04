using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc;

public interface IProtocol
{
    ICommand GetCommand(string name);
    Dictionary<string, ICommand> GetCommands();
    void AddCommand(ICommand command, string name);
    void FlushCommands();
    EnumProtocolType GetProtocolType();
    string FormattedUser(IChannelMember user);
    string GetFormat(IUser user);
    void UpdateCommand(ICommand command, string name);
}