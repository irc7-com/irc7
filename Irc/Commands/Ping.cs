using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

public class Ping : Command, ICommand
{
    public Ping() : base(1, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        chatFrame.User.Send($"PONG :{chatFrame.ChatMessage.Parameters.First()}");
    }
}