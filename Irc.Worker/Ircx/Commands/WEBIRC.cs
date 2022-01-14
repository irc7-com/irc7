﻿using Irc.Worker.Ircx.Objects;

namespace Irc.Worker.Ircx.Commands;

// For webchat proxies
public class WEBIRC : Command
{
    //WEBIRC <passwd> ircgw localhost 127.0.0.1 :6s
    public WEBIRC(CommandCode Code) : base(Code)
    {
        MinParamCount = 0; // to suppress any warnings
        DataType = CommandDataType.None;
    }

    public new bool Execute(Frame Frame)
    {
        if (Frame.Message.Parameters != null)
        {
            if (Frame.Message.Parameters.Count >= 4)
            {
                var Password = Frame.Message.Parameters[0];
                var Username = Frame.Message.Parameters[1];
                var Hostname = Frame.Message.Parameters[2];
                var IP = Frame.Message.Parameters[3];

                if (Username == Program.Config.WebIRCUsername && Password == Program.Config.WebIRCPassword)
                {
                    Frame.User.Address.Host = Hostname;
                    Frame.User.RemoteIP = IP;
                }
            }

            if (Frame.Message.Parameters.Count == 5)
                if (Frame.Message.Parameters[4].Contains('s'))
                    //Set secure mode
                    Frame.User.Modes.Secure.Value = 1;
        }

        return true;
    }
}