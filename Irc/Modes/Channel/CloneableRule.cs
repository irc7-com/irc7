using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;

public class CloneableRule : ModeRuleChannel, IModeRule
{
    public CloneableRule() : base(Resources.ChannelModeCloneable)
    {
    }

    /// <summary>
    /// Tracks the numeric suffix of the current active clone channel (1–99).
    /// Starts at 1 (first clone) and advances only when the current clone becomes full.
    /// This avoids scanning all 99 slots on every join attempt.
    /// </summary>
    public int NextCloneIndex { get; set; } = 1;

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        // Per draft-pfenning-irc-extensions-04 section 8.1.16:
        // It is not valid to set the CLONEABLE channel mode of a parent channel that ends with a numeric character.
        if (flag && char.IsDigit(((IChannel)target).GetName().Last()))
            return EnumIrcError.ERR_NOPERMS;

        return EvaluateAndSet(source, target, flag, parameter);
    }
}