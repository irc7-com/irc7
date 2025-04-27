using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Modes.Channel;

public class SecretRule : ModeRuleChannel, IModeRule
{
    public SecretRule() : base(Resources.ChannelModeSecret)
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
                if (channel.Modes.Private.ModeValue)
                {
                    channel.Modes.Private.ModeValue = false;
                    DispatchModeChange(Resources.ChannelModePrivate, source, target, false, string.Empty);
                }

                if (channel.Modes.Hidden.ModeValue)
                {
                    channel.Modes.Hidden.ModeValue = false;
                    DispatchModeChange(Resources.ChannelModeHidden, source, target, false, string.Empty);
                }
            }

            SetChannelMode(source, (IChannel)target, flag, parameter);
        }

        return result;
    }
}