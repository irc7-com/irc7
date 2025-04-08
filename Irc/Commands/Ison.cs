using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

internal class Ison : Command, ICommand
{
    public Ison() : base(1)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Data;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = chatFrame.Server;
        var user = chatFrame.User;
        var parameters = chatFrame.ChatMessage.Parameters;

        var nicknames = parameters.Distinct(StringComparer.InvariantCultureIgnoreCase).ToList();
        var foundNicknames = new List<string>();

        foreach (var nickname in nicknames)
        {
            var found = chatFrame.Server.GetUsers().FirstOrDefault(serverUser =>
                serverUser.Name.ToUpperInvariant() == nickname.ToUpperInvariant()) != null;
            if (found) foundNicknames.Add(nickname);
        }

        user.Send(Raws.IRC_RAW_303(server, user, string.Join(' ', foundNicknames)));
    }
}