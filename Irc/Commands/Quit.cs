using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

internal class Quit : Command, ICommand
{
    public Quit() : base(0, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = chatFrame.Server;
        var user = chatFrame.User;

        var quitMessage = Resources.CONNRESETBYPEER;
        if (chatFrame.Message.Parameters.Count > 0) quitMessage = chatFrame.Message.Parameters.First();

        QuitChannels(user, quitMessage);
    }

    public static void QuitChannels(IUser user, string message)
    {
        var users = new HashSet<IUser>();

        var channels = user.GetChannels().Keys;

        foreach (var channel in channels)
        {
            foreach (var member in channel.GetMembers()) users.Add(member.GetUser());
            channel.Quit(user);
        }

        user.GetChannels().Clear();

        var quitRaw = Raws.RPL_QUIT(user, message);

        foreach (var targetUser in users) targetUser.Send(quitRaw);
        user.Disconnect(quitRaw);
    }
}