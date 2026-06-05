using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;
using Irc.Services;

namespace Irc.Directory.Daemon.Commands;

internal class Listx : Command, ICommand
{
    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    // Below is custom LISTX command for Directory
    // It outputs LIST raws but allows for more complex filtering of rooms, as well as categories
    // LISTX [category code] [query list=[mask]]
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

        if (parameters.Count > 0)
        {
            if (!FilterByCategory(chatFrame, server, user, ref rooms)) return;
        }

        if (parameters.Count > 1)
        {
            if (!FilterByQueryList(chatFrame, server, user, ref rooms)) return;
        }

        // For some reason, don't ask me why, they implemented LISTX as LIST on the Directory Server
        user.Send($":{server} 321 {user} Channel :Users Name");

        foreach (var room in rooms)
        {
            user.Send($":{server} 322 {user} {room.Name} {room.CurrentUsers} :{room.Topic}");
        }

        user.Send($":{server} 323 {user} :End of /LIST");
    }

    private static bool FilterByQueryList(IChatFrame chatFrame, DirectoryServer server, IUser user, ref List<AcsRoomInfo> rooms)
    {
        for (var i = 1; i < chatFrame.ChatMessage.Parameters.Count; i++)
        {
            var term =  chatFrame.ChatMessage.Parameters[i];
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
                var mask = term.Substring(2).ToUpper();
                rooms = rooms.Where(r => r.Name.ToUpper().Contains(mask)).ToList();
            }
            else if (term.StartsWith("T="))
            {
                var mask = term.Substring(2).ToUpper();
                rooms = rooms.Where(r => r.Topic.ToUpper().Contains(mask)).ToList();
            }
            else if (term.StartsWith("Q=") && uint.TryParse(term.Substring(2), out var queryLimit))
            {
                if (queryLimit == 0)
                {
                    chatFrame.User.Send(Raws.IRCX_ERR_BADCOMMAND_900(server, user, "LISTX"));
                    return false;
                }
                rooms = rooms.Take(queryLimit > int.MaxValue ? int.MaxValue : (int)queryLimit).ToList();
            }
            else
            {
                // Invalid query term, return error
                chatFrame.User.Send(Raws.IRCX_ERR_BADCOMMAND_900(server, user, "LISTX"));
                return false;
            }
        }

        return true;
    }

    private static bool FilterByCategory(IChatFrame chatFrame, DirectoryServer server, IUser user, ref List<AcsRoomInfo> rooms)
    {
        var catcode = chatFrame.ChatMessage.Parameters[0].ToUpper();
        // If it is a category
        var category = Resources.ChannelCategoryNames.FirstOrDefault(c => c.Key.ToUpper() == catcode);
        if (category.Key == null)
        {
            // No such category
            chatFrame.User.Send(Raws.IRCX_RPL_FINDS_NOSUCHCAT_701(server, user));
            return false;
        }
            
        // Process category
        rooms = rooms.Where(r => r.Category == category.Key).ToList();
        return true;
    }
}