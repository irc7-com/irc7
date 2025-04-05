using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Modes.Channel;

public class Private : ModeRuleChannel, IModeRule
{
    public Private() : base(Resources.ChannelModePrivate)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        var result = base.Evaluate(source, target, flag, parameter);
        if (result == EnumIrcError.OK)
        {
            var channel = (IChannel)target;

            if (flag)
            {
                if (channel.Modes.Secret)
                {
                    channel.Modes.Secret = false;
                    DispatchModeChange(Resources.ChannelModeSecret, source, target, false, string.Empty);
                }

                if (channel.Modes.Hidden)
                {
                    channel.Modes.Hidden = false;
                    DispatchModeChange(Resources.ChannelModeHidden, source, target, false, string.Empty);
                }
            }

            SetChannelMode(source, (IChannel)target, flag, parameter);
        }

        return result;
    }
}