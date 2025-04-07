using Irc;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;

public class Clone : ModeRuleChannel, IModeRule
{
    public Clone() : base(ExtendedResources.ChannelModeClone)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EvaluateAndSet(source, target, flag, parameter);
    }
}