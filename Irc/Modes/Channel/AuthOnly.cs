using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;

public class AuthOnly : ModeRuleChannel, IModeRule
{
    public AuthOnly() : base(Resources.ChannelModeAuthOnly)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EvaluateAndSet(source, target, flag, parameter);
    }
}