using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Modes.Channel;

public class Hidden : ModeRuleChannel, IModeRule
{
    public Hidden() : base(Resources.ChannelModeHidden)
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

                if (channel.Modes.Private)
                {
                    channel.Modes.Private = false;
                    DispatchModeChange(Resources.ChannelModePrivate, source, target, false, string.Empty);
                }
            }

            SetChannelMode(source, (IChannel)target, flag, parameter);
        }

        return result;
    }
}