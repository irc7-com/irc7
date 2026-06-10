using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;

public class AuthOnlyRule : ModeRuleChannel, IModeRule
{
    public AuthOnlyRule() : base(Resources.ChannelModeAuthOnly)
    {
    }

    // Can be set by server sysop managers or channel owners only.
    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        var user = (IUser)source;
        var channel = (IChannel)target;
        var userLevel = user.GetLevel();
        
        // Can only be set by sysop or above, OR during channel creation
        if (userLevel < EnumUserAccessLevel.Sysop) return EnumIrcError.ERR_NOIRCOP;

        SetChannelMode(source, channel, flag, parameter);
        return EnumIrcError.OK;
    }

    // Readable by sysops, channel hosts, and members always.
    // Non-members may only read it on PUBLIC or HIDDEN channels (not PRIVATE or SECRET).
    public override bool CanRead(IUser user, IChannel channel)
    {
        if (user.GetLevel() >= EnumUserAccessLevel.Sysop) return true;
        if (channel.HasUser(user)) return true;
        return !channel.Modes.Private.ModeValue && !channel.Modes.Secret.ModeValue;
    }
}
