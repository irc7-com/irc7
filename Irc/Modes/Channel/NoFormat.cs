using Irc;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;

public class NoFormat : ModeRuleChannel, IModeRule
{
    public NoFormat() : base(ExtendedResources.ChannelModeProfanity)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EvaluateAndSet(source, target, flag, parameter);
    }
}