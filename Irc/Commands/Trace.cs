﻿using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

internal class Trace : Command, ICommand
{
    public Trace() : base()
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        chatFrame.User.Send(Raw.IRCX_ERR_COMMANDUNSUPPORTED_554(chatFrame.Server, chatFrame.User, nameof(Trace)));
    }
}