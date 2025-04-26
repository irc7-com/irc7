using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

internal class Ircx : Command, ICommand
{
    public Ircx() : base(0, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        // TODO: This code is really ugly and needs cleaning up
        var protocol = chatFrame.User.GetProtocol().GetProtocolType();
        if (protocol < EnumProtocolType.IRCX)
        {
            protocol = EnumProtocolType.IRCX;
            var nominatedProtocol = chatFrame.Server.GetProtocol(protocol);
            if (nominatedProtocol == null) throw new Exception("Ircx protocol could not be found.");
            chatFrame.User.SetProtocol(nominatedProtocol);
        }

        chatFrame.User.Modes.ToggleModeValue(Resources.UserModeIrcx, true);

        chatFrame.User.Send(Raws.IRCX_RPL_IRCX_800(chatFrame.Server, chatFrame.User, 1, 0,
            chatFrame.Server.MaxInputBytes, Resources.IRCXOptions));
    }
}