using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

public class Admin : Command, ICommand
{
    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        /*
         <- :sky-8a15b323126 256 Sky :Administrative info about sky-8a15b323126
         <- :sky-8a15b323126 257 Sky :This is a message about Administrator information
         <- :sky-8a15b323126 258 Sky :This is the second line about Admin
         <- :sky-8a15b323126 259 Sky :
        */
        var adminInfo1 = chatFrame.Server.GetDataStore().Get(Resources.ConfigAdminInfo1);
        var adminInfo2 = chatFrame.Server.GetDataStore().Get(Resources.ConfigAdminInfo2);
        var adminInfo3 = chatFrame.Server.GetDataStore().Get(Resources.ConfigAdminInfo3);

        var hasAdminInfo = !string.IsNullOrWhiteSpace(adminInfo1);

        if (hasAdminInfo)
        {
            chatFrame.User.Send(Raws.IRC_RAW_256(chatFrame.Server, chatFrame.User));
            chatFrame.User.Send(Raws.IRC_RAW_257(chatFrame.Server, chatFrame.User, adminInfo1));
            chatFrame.User.Send(Raws.IRC_RAW_258(chatFrame.Server, chatFrame.User, adminInfo2));
            chatFrame.User.Send(Raws.IRC_RAW_259(chatFrame.Server, chatFrame.User, adminInfo3));
        }
        else
        {
            // <- :sky-8a15b323126 423 Sky sky-8a15b323126 :No administrative info available
            chatFrame.User.Send(Raws.IRC_RAW_423(chatFrame.Server, chatFrame.User));
        }
    }
}