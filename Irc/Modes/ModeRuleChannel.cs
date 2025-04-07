using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;
using Irc.Objects.Channel;

namespace Irc.Modes;

public class ModeRuleChannel : ModeRule, IModeRule
{
    private readonly EnumChannelAccessLevel accessLevel;

    public ModeRuleChannel(char modeChar, bool requiresParameter = false, int initialValue = 0,
        EnumChannelAccessLevel accessLevel = EnumChannelAccessLevel.ChatHost) :
        base(modeChar, requiresParameter, initialValue)
    {
        this.accessLevel = accessLevel;
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        var user = (IUser)source;
        var channel = (IChannel)target;
        var member = channel.GetMember(user);

        if (member == null && !user.IsAdministrator()) return EnumIrcError.ERR_NOTONCHANNEL;

        if (member?.GetLevel() < accessLevel) return EnumIrcError.ERR_NOCHANOP;

        return EnumIrcError.OK;
    }

    public EnumIrcError EvaluateAndSet(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        var result = Evaluate(source, target, flag, parameter);
        if (result == EnumIrcError.OK) SetChannelMode(source, (IChannel)target, flag, parameter);
        return result;
    }

    public void SetChannelMode(IChatObject source, IChannel target, bool flag, string parameter)
    {
        var channelModes = (ChannelModes)target.Modes;
        channelModes[ModeChar].Set(Convert.ToInt32(flag));
        DispatchModeChange(source, (ChatObject)target, flag, parameter);
    }
}