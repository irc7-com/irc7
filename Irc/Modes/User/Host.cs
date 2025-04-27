using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Modes.User;

public class Host : ModeRuleChannel, IModeRule
{
    public Host() : base(Resources.UserModeHost, true)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        // TODO: Write this better
        if (target == source && flag)
        {
            if (string.IsNullOrWhiteSpace(parameter)) return EnumIrcError.OK;

            var user = (IUser)source;
            var channel = user.GetChannels().LastOrDefault().Key;
            var channelModes = channel.Modes;
            var member = user.GetChannels().LastOrDefault().Value;

            var ownerkeyProp = channel.Props.OwnerKey;
            var hostkeyProp = channel.Props.HostKey;

            if (ownerkeyProp.GetValue(target) == parameter)
            {
                if (member.Operator.ModeValue)
                {
                    member.Operator.ModeValue = false;
                    DispatchModeChange(Resources.MemberModeHost, source, (IChatObject)channel, false,
                        target.ToString());
                }

                member.Owner.ModeValue = true;
                DispatchModeChange(Resources.MemberModeOwner, source, (IChatObject)channel, true, target.ToString());
            }
            else if (hostkeyProp.GetValue(target) == parameter)
            {
                if (member.Owner.ModeValue)
                {
                    member.Owner.ModeValue = false;
                    DispatchModeChange(Resources.MemberModeOwner, source, (IChatObject)channel, false,
                        target.ToString());
                }

                member.Operator.ModeValue = true;
                DispatchModeChange(Resources.MemberModeHost, source, (IChatObject)channel, true, target.ToString());
            }

            return EnumIrcError.OK;
        }

        return EnumIrcError.ERR_UNKNOWNMODEFLAG;
    }
}