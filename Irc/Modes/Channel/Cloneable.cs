using Irc;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;

public class Cloneable : ModeRuleChannel, IModeRule
{
    public Cloneable() : base(ExtendedResources.ChannelModeCloneable)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EvaluateAndSet(source, target, flag, parameter);
    }
}