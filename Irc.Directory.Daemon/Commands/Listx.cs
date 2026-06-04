using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;

namespace Irc.Directory.Daemon.Commands;

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
                else
                {
                    // If it is a category
                    var category = Resources.ChannelCategoryNames.FirstOrDefault(c => c.Key == term);
                    if (category.Key != null)
                    {
                        // Process category
                        rooms = rooms.Where(r => r.Category == category.Key).ToList();
                    }
                }
            }
        }

        // For some reason, don't ask me why, they implemented LISTX as LIST on the Directory Server
        user.Send($":{server} 321 {user} Channel :Users Name");

        foreach (var room in rooms)
        {
            user.Send($":{server} 322 {user} {room.Name} {room.CurrentUsers} :{room.Topic}");
        }

        user.Send($":{server} 323 {user} :End of /LIST");
    }
}