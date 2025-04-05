using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc.Modes.Channel.Member;

public class Voice : ModeRule, IModeRule
{
    public Voice() : base(Resources.MemberModeVoice, true)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        // TODO: Consider merging the below two blocks
        var channel = (IChannel)target;
        if (!channel.CanBeModifiedBy(source)) return EnumIrcError.ERR_NOTONCHANNEL;

        var sourceMember = channel.GetMember((IUser)source);
        if (sourceMember == null) return EnumIrcError.ERR_NOTONCHANNEL;
        
        var targetMember = channel.GetMemberByNickname(parameter);
        if (targetMember == null) return EnumIrcError.ERR_NOSUCHNICK;

        var result = sourceMember.CanModify(targetMember, EnumChannelAccessLevel.ChatVoice, false);
        if (result == EnumIrcError.OK)
        {
            targetMember.SetVoice(flag);
            DispatchModeChange(source, target, flag, targetMember.GetUser().ToString());
        }

        return result;
    }
}