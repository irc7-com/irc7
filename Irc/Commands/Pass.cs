using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

public class Pass : Command, ICommand
{
    public Pass() : base(1, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        if (!chatFrame.User.IsRegistered())
            // TODO: Encrypt below pass
            chatFrame.User.Pass = chatFrame.ChatMessage.Parameters.First();
        else
            chatFrame.User.Send(Raws.IRCX_ERR_ALREADYREGISTERED_462(chatFrame.Server, chatFrame.User));
    }
}