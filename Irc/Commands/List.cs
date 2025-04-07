using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

internal class List : Command, ICommand
{
    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Data;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = chatFrame.Server;
        var user = chatFrame.User;
        var parameters = chatFrame.ChatMessage.Parameters;

        var channels = server.GetChannels().Where(c => !c.Modes.Secret).ToList();
        if (parameters.Count > 0)
        {
            var channelNames = parameters.First().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            channels = server
                .GetChannels()
                .Where(c => !c.Modes.Secret
                            && channelNames.Contains(c.GetName(), StringComparer.InvariantCultureIgnoreCase)).ToList();
        }

        ListChannels(server, user, channels);
    }

    public void ListChannels(IServer server, IUser user, IList<IChannel> channels)
    {
        user.Send(Raws.IRCX_RPL_MODE_321(server, user));
        foreach (var channel in channels) user.Send(Raws.IRCX_RPL_MODE_322(server, user, channel));
        user.Send(Raws.IRCX_RPL_MODE_323(server, user));
    }
}