using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;

internal class Away : Command, ICommand
{
    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = chatFrame.Server;
        var user = chatFrame.User;
        if (chatFrame.Message.Parameters.Count == 0)
        {
            user.SetBack(server, chatFrame.User);
            return;
        }

        var reason = chatFrame.Message.Parameters.First();
        user.SetAway(server, chatFrame.User, reason);
    }
}