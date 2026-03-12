using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;

public class CloneRule : ModeRuleChannel, IModeRule
{
    public CloneRule() : base(Resources.ChannelModeClone)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        // Per draft-pfenning-irc-extensions-04 section 8.1.17:
        // CLONE mode can only be set by the sysop manager (during channel creation).
        // Regular users (even channel operators) cannot set this mode manually.
        var user = (IUser)source;
        if (user.GetLevel() < EnumUserAccessLevel.Sysop)
            return EnumIrcError.ERR_NOPERMS;

        return EvaluateAndSet(source, target, flag, parameter);
    }
}