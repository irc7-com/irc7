using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc.Modes.Channel.Member;

public class Operator : ModeRule, IModeRule
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
    public Operator() : base(Resources.MemberModeHost, true)
    {
    }

    public EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        // TODO: Consider combining below blocks
        var channel = (IChannel)target;
        if (!channel.CanBeModifiedBy(source)) return EnumIrcError.ERR_NOTONCHANNEL;
        
        var sourceMember = channel.GetMember((IUser)source);
        if (sourceMember == null) return EnumIrcError.ERR_NOTONCHANNEL;
        
        var targetMember = channel.GetMemberByNickname(parameter);
        if (targetMember == null) return EnumIrcError.ERR_NOSUCHNICK;

        var result = sourceMember.CanModify(targetMember, EnumChannelAccessLevel.ChatHost);
        if (result != EnumIrcError.OK) return result;

        if (targetMember.IsOwner())
        {
            targetMember.SetOwner(false);
            DispatchModeChange(Resources.MemberModeOwner, source, target, false, targetMember.GetUser().ToString());
        }

        targetMember.SetHost(flag);
        DispatchModeChange(source, target, flag, targetMember.GetUser().ToString());
        return result;
    }
}