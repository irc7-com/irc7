using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc.Modes.Channel;

internal class Key : ModeRuleChannel, IModeRule
{
    public Key() : base(Resources.ChannelModeKey, true)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        var channel = (IChannel)target;
        var member = channel.GetMember((IUser)source);
        if (member?.GetLevel() >= EnumChannelAccessLevel.ChatHost)
        {
            // Unset key
            if (!flag && parameter == channel.Modes.Key)
            {
                channel.Modes.Key = null;
                DispatchModeChange(source, (ChatObject)target, flag, parameter);
                return EnumIrcError.OK;
            }

            // Set key
            if (flag)
            {
                if (!string.IsNullOrWhiteSpace(channel.Modes.Key)) return EnumIrcError.ERR_KEYSET;

                channel.Modes.Key = flag ? parameter : null;
                DispatchModeChange(source, (ChatObject)target, flag, parameter);
            }

            return EnumIrcError.OK;
        }

        /* -> sky-8a15b323126 MODE #test +t
            <- :sky-8a15b323126 482 Sky2k #test :You're not channel operator */
        return EnumIrcError.ERR_NOCHANOP;
    }
}