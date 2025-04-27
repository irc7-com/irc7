using System.Security.Cryptography.X509Certificates;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc.Modes.Channel;

public class Key : ModeRuleChannel, IModeRule
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
            if (!flag && parameter == channel.Props.MemberKey.Value)
            {
                channel.Modes.Key.ModeValue = false;
                channel.Modes.Keypass = string.Empty;
                channel.Props.MemberKey.Value = string.Empty;
                DispatchModeChange(source, (ChatObject)target, flag, parameter);
                return EnumIrcError.OK;
            }

            // Set key
            if (flag)
            {
                if (!string.IsNullOrWhiteSpace(channel.Props.MemberKey.Value)) return EnumIrcError.ERR_KEYSET;

                channel.Modes.Key.ModeValue = true;
                channel.Modes.Keypass = parameter;
                channel.Props.MemberKey.Value = parameter;
                DispatchModeChange(source, (ChatObject)target, flag, parameter);
            }

            return EnumIrcError.OK;
        }

        /* -> sky-8a15b323126 MODE #test +t
            <- :sky-8a15b323126 482 Sky2k #test :You're not channel operator */
        return EnumIrcError.ERR_NOCHANOP;
    }
}