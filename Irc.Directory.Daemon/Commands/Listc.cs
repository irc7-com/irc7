using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Directory.Commands;

internal class Listc : Command, ICommand
{
    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = chatFrame.Server;
        var user = chatFrame.User;

        if (user.GetLevel() < EnumUserAccessLevel.Guide)
        {
            user.Send(Raws.IRCX_ERR_SECURITY_908(server, user));
            return;
        }

        var isIrc4Plus = user.GetProtocol().GetProtocolType() >= EnumProtocolType.IRC4;

        user.Send(Raws.IRCX_RPL_LISTCSTART_610(server, user));

        foreach (var category in Resources.SupportedChannelCategories)
        {
            if (isIrc4Plus)
            {
                if (!Resources.ChannelCategoryNames.TryGetValue(category, out var name))
                    name = category;
                user.Send(Raws.IRCX_RPL_LISTCLIST_IRC4_611(server, user, category, name));
            }
            else
            {
                user.Send(Raws.IRCX_RPL_LISTCLIST_611(server, user, category));
            }
        }

        user.Send(Raws.IRCX_RPL_LISTCEND_612(server, user));
    }
}
