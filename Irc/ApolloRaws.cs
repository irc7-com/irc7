using Irc.Interfaces;

namespace Irc;

public static class ApolloRaws
{
    public static string RPL_JOIN_MSN(IChannelMember channelMember, IChannel channel, IChannelMember joinMember)
    {
        var listedMode = joinMember.GetListedMode();
        var listedModeString = !string.IsNullOrWhiteSpace(listedMode) ? $",{listedMode}" : "";
        return
            $":{joinMember.GetUser().GetAddress()} JOIN {channelMember.GetUser().GetProtocol().GetFormat(joinMember.GetUser())}{listedModeString} :{channel}";
    }

    public static string RPL_EPRIVMSG(IUser user, IChannel channel, string message)
    {
        return $":{user.GetAddress()} EPRIVMSG {channel} :{message}";
    }

    public static string RPL_EQUESTION(IUser user, IChannel channel, string nickname, string message)
    {
        return $":{user.GetAddress()} EQUESTION {channel} {nickname} {channel} :{message}";
    }
}