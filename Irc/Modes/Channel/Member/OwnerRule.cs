﻿using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Modes.Channel.Member;

public class OwnerRule : ModeRule, IModeRule
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
    public OwnerRule() : base(Resources.MemberModeOwner, true)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        var channel = (IChannel)target;

        // TODO: Merge the below two blocks
        if (!channel.CanBeModifiedBy(source)) return EnumIrcError.ERR_NOTONCHANNEL;

        var sourceMember = channel.GetMember((IUser)source);
        if (sourceMember == null) return EnumIrcError.ERR_NOTONCHANNEL;

        var targetMember = channel.GetMemberByNickname(parameter);
        if (targetMember == null) return EnumIrcError.ERR_NOSUCHNICK;

        var result = sourceMember.CanModify(targetMember, EnumChannelAccessLevel.ChatOwner);
        if (result == EnumIrcError.OK)
        {
            if (flag && targetMember.Operator.ModeValue)
            {
                targetMember.Operator.ModeValue = false;
                DispatchModeChange(Resources.MemberModeHost, source, target, false, targetMember.GetUser().ToString());
            }

            targetMember.Owner.ModeValue = flag;
            DispatchModeChange(source, target, flag, targetMember.GetUser().ToString());
        }

        return result;
    }
}