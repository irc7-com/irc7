using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

public class Version : Command, ICommand
{
    public Version() : base(0, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        chatFrame.User.Send(Raws.IRCX_RPL_VERSION_351(chatFrame.Server, chatFrame.User,
            chatFrame.Server.ServerVersion));
    }
}