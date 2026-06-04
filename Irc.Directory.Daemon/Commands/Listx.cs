using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;
using Irc.Services;

namespace Irc.Directory.Commands;

internal class Listx : Command, ICommand
{
    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = (DirectoryServer)chatFrame.Server;
        var user = chatFrame.User;

        if (user.GetLevel() < EnumUserAccessLevel.Guide)
        {
            user.Send(Raws.IRCX_ERR_SECURITY_908(server, user));
            return;
        }

        var parameters = chatFrame.ChatMessage.Parameters;
        var rooms = server.CacheManager.GetAllRooms().ToList();
        var firstParam = parameters.FirstOrDefault();

        if (firstParam != null)
        {
            var queryTerms = Tools.CSVToArray(firstParam);
            foreach (var term in queryTerms)
            {
                if (term == "R=0")
                {
                    rooms = rooms.Where(r => !r.Managed).ToList();
                }
                else if (term == "R=1")
                {
                    rooms = rooms.Where(r => r.Managed).ToList();
                }
                else if (term.StartsWith("N="))
                {
                    var mask = term.Substring(2);
                    rooms = rooms.Where(r => Tools.MatchesMask(r.Name, mask)).ToList();
                }
                else if (term.StartsWith("T="))
                {
                    var mask = term.Substring(2);
                    rooms = rooms.Where(r => Tools.MatchesMask(r.Topic, mask)).ToList();
                }
                else if (term.StartsWith("Q=") && int.TryParse(term.Substring(2), out var queryLimit))
                {
                    rooms = rooms.Take(queryLimit).ToList();
                }
            }
        }

        user.Send(Raws.IRCX_RPL_LISTXSTART_811(server, user));

        foreach (var room in rooms)
        {
            user.Send(Raws.IRCX_RPL_LISTXLIST_812(
                server,
                user,
                room.Name,
                room.Modes,
                room.CurrentUsers,
                room.MaxUsers,
                room.Topic
            ));
        }

        user.Send(Raws.IRCX_RPL_LISTXEND_817(server, user));
    }
}
