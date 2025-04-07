using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

public class Users : Command, ICommand
{
    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        // -> sky-8a15b323126 USERS
        // < - :sky - 8a15b323126 446 Sky2k: USERS has been disabled

        chatFrame.User.Send(IrcRaws.IRC_RAW_446(chatFrame.Server, chatFrame.User));
    }
}