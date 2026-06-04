using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
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

        var substring = chatFrame.ChatMessage.Parameters.FirstOrDefault();

        IEnumerable<AcsRoomInfo> rooms = server.CacheManager.GetAllRooms();

        if (!string.IsNullOrEmpty(substring))
        {
            rooms = rooms.Where(r => r.Name.Contains(substring, StringComparison.OrdinalIgnoreCase));
        }

        var roomList = rooms.ToList();

        user.Send(Raws.IRCX_RPL_LISTXSTART_811(server, user));

        foreach (var room in roomList)
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
