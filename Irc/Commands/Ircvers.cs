using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

internal class Ircvers : Command, ICommand
{
    public Ircvers() : base(2, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Standard;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        //return;
        if (chatFrame.User.IsRegistered())
        {
            chatFrame.User.Send(Raws.IRCX_ERR_ALREADYREGISTERED_462(chatFrame.Server, chatFrame.User));
        }
        else
        {
            var ircvers = chatFrame.ChatMessage.Parameters[0].ToUpper();

            if (ircvers.Length == 4 && ircvers.StartsWith("IRC") && char.IsNumber(ircvers.Last()))
            {
                if (Enum.TryParse<EnumProtocolType>(ircvers, true, out var enumProtocolType))
                {
                    if (chatFrame.Server.GetProtocols().TryGetValue(enumProtocolType, out var protocol))
                    {
                        chatFrame.User.SetProtocol(protocol);
                        chatFrame.User.Client = chatFrame.ChatMessage.Parameters[1];

                        var isircx = protocol.GetProtocolType() > EnumProtocolType.IRC;
                        chatFrame.User.Send(Raws.IRCX_RPL_IRCX_800(chatFrame.Server, chatFrame.User, isircx ? 1 : 0, 0,
                            chatFrame.Server.MaxInputBytes, Resources.IRCXOptions));
                    }
                    else
                    {
                        chatFrame.User.Send(Raws.IRCX_ERR_BADVALUE_906(chatFrame.Server, chatFrame.User, ircvers));
                    }
                }

                return;
            }

            chatFrame.User.Send(Raws.IRCX_ERR_BADVALUE_906(chatFrame.Server, chatFrame.User, ircvers));
        }
    }
}