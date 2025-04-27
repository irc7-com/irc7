using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;

public class NoWhisperRule : ModeRuleChannel, IModeRule
{
    public NoWhisperRule() : base(Resources.ChannelModeNoWhisper)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EvaluateAndSet(source, target, flag, parameter);
    }
}