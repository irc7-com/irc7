﻿using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

internal class Isircx : Command, ICommand
{
    public Isircx() : base(0, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        chatFrame.User.Send(Raws.IRCX_ERR_NOTIMPLEMENTED(chatFrame.Server, chatFrame.User, nameof(Access)));
    }
}