using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

public class Time : Command, ICommand
{
    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        chatFrame.User.Send(Raw.IRCX_RPL_TIME_391(chatFrame.Server, chatFrame.User));
    }
}