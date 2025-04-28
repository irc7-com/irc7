using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc.Modes.Channel.Member;

public class OperatorRule : ModeRule, IModeRule
{
    /*
     -> sky-8a15b323126 MODE #test +q Sky2k
    <- :sky-8a15b323126 482 Sky3k #test :You're not channel operator
    -> sky-8a15b323126 MODE #test +o Sky2k
    <- :sky-8a15b323126 482 Sky3k #test :You're not channel operator
    <- :Sky2k!~no@127.0.0.1 MODE #test +o Sky3k
    -> sky-8a15b323126 MODE #test +q Sky2k
    <- :sky-8a15b323126 485 Sky3k #test :You're not channel owner
    -> sky-8a15b323126 MODE #test +o Sky2k
    <- :sky-8a15b323126 485 Sky3k #test :You're not channel owner
     */
    public OperatorRule() : base(Resources.MemberModeHost, true)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        // TODO: Consider combining below blocks
        var channel = (IChannel)target;
        var user = (IUser)source;

        var sourceMember = channel.GetMember(user);
        if (sourceMember == null || !channel.CanBeModifiedBy((ChatObject)source)) return EnumIrcError.ERR_NOTONCHANNEL;

        var targetMember = channel.GetMemberByNickname(parameter);
        if (targetMember == null) return EnumIrcError.ERR_NOSUCHNICK;

        var result = sourceMember.CanModify(targetMember, EnumChannelAccessLevel.ChatHost);
        if (result != EnumIrcError.OK) return result;

        if (targetMember.Owner.ModeValue)
        {
            targetMember.Owner.ModeValue = false;
            DispatchModeChange(Resources.MemberModeOwner, source, target, false, targetMember.GetUser().ToString());
        }

        targetMember.Operator.ModeValue = flag;
        DispatchModeChange(source, target, flag, targetMember.GetUser().ToString());
        return result;
    }
}