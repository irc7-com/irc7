using Irc.Commands;
using Irc.Interfaces;

namespace Irc.Extensions.Apollo.Commands;

using IrcNotice = global::Irc.Commands.Privmsg;

public class Notice : Privmsg, ICommand
{
    public new void Execute(IChatFrame chatFrame)
    {
        SendMessage(chatFrame, true);
    }
}