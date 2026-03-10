using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;

public class CloneableRule : ModeRuleChannel, IModeRule
{
    public CloneableRule() : base(Resources.ChannelModeCloneable)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        // Per draft-pfenning-irc-extensions-04 section 8.1.16:
        // It is not valid to set the CLONEABLE channel mode of a parent channel that ends with a numeric character.
        if (flag && char.IsDigit(((IChannel)target).GetName().Last()))
            return EnumIrcError.ERR_NOPERMS;

        return EvaluateAndSet(source, target, flag, parameter);
    }
}