using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc.Modes.Channel;

public class UserLimit : ModeRuleChannel, IModeRule
{
    public UserLimit() : base(Resources.ChannelModeUserLimit, true)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        var result = base.Evaluate(source, target, flag, parameter);
        if (result != EnumIrcError.OK) return result;

        var user = (IUser)source;
        var channel = (IChannel)target;
        var isAdministrator = user.IsAdministrator();
        var channelModes = (ChannelModes)channel.Modes;

        if (flag == false)
        {
            if (isAdministrator)
            {
                // TODO: Currently does not support unsetting limit without extra parameter
                channelModes.UserLimit = 0;
                DispatchModeChange(source, target, false, string.Empty);
            }

            return EnumIrcError.OK;
        }


        if (!int.TryParse(parameter, out var limit)) return EnumIrcError.ERR_NEEDMOREPARAMS;

        if (limit > 0 && (limit <= 100 || isAdministrator))
        {
            channelModes.UserLimit = limit;
            DispatchModeChange(source, target, true, limit.ToString());
        }

        return EnumIrcError.OK;
    }
}