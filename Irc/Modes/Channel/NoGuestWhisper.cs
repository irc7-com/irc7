using Irc;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;

public class NoGuestWhisper : ModeRuleChannel, IModeRule
{
    public NoGuestWhisper() : base(ExtendedResources.ChannelModeNoGuestWhisper)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EvaluateAndSet(source, target, flag, parameter);
    }
}