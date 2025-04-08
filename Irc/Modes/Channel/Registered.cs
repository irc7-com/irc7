using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;

public class Registered : ModeRuleChannel, IModeRule
{
    public Registered() : base(Resources.ChannelModeRegistered)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EvaluateAndSet(source, target, flag, parameter);
    }
}