using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

internal class Part : Command, ICommand
{
    public Part() : base(1)
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
        var parameters = chatFrame.Message.Parameters.First();

        var channelNames = Join.ValidateChannels(server, user, parameters);
        if (channelNames.Count == 0) return;

        PartChannels(server, user, channelNames);
    }

    public void PartChannels(IServer server, IUser user, List<string> channelNames)
    {
        server
            .GetChannels()
            .Where(c => channelNames.Contains(c.GetName()))
            .ToList()
            .ForEach(
                channel =>
                {
                    if (!user.IsOn(channel))
                    {
                        user.Send(Raw.IRCX_ERR_NOTONCHANNEL_442(server, user, channel));

                        return;
                    }

                    channel.Part(user);
                    user.RemoveChannel(channel);
                }
            );
    }
}