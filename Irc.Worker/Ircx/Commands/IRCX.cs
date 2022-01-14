﻿using Irc.Constants;
using Irc.Extensions.Security;
using Irc.Worker.Ircx.Objects;

namespace Irc.Worker.Ircx.Commands;

internal class IRCX : Command
{
    //         800 - IRCRPL_IRCX

    // <state> <version> <package-list> <maxmsg> <option-list>

    // The response to the IRCX and ISIRCX commands.The<state>
    // indicates if the client has IRCX mode enabled (0 for disabled,
    // 1 for enabled).  The<version> is the version of the IRCX
    //protocol starting at 0.   The<package-list> contains a list
    // of authentication packages supported by the server.The
    // package name of "ANON" is reserved to indicate that anonymous
    // connections are permitted.The<maxmsg> defines the maximum
    // message size permitted, with the standard being 512. The
    // <option-list> contains a list of options supported by the
    // server; these are implementation-dependent.If no options are
    // available, the  '*'  character is used.
    public IRCX(CommandCode Code) : base(Code)
    {
        MinParamCount = 0;
        DataType = CommandDataType.Standard;
        ForceFloodCheck = true;
    }

    public static void ProcessIRCXReply(Frame Frame)
    {
        Frame.User.Send(RawBuilder.Create(Frame.Server, Client: Frame.User, Raw: Raws.IRCX_RPL_IRCX_800,
            Data: new[] {Program.Providers.SupportedPackages, Resources.IRCXOptions},
            IData: new[] {Frame.User.Modes.Ircx.Value, Frame.Server.ServerFields.IrcxVersion, Program.Config.BufferSize}));
    }

    public new bool Execute(Frame Frame)
    {
        Frame.User.Modes.Ircx.Value = 1;
        ProcessIRCXReply(Frame);
        return true;
    }
}