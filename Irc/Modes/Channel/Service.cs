using Irc;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;

public class Service : ModeRuleChannel, IModeRule
{
    public Service() : base(ExtendedResources.ChannelModeService)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EvaluateAndSet(source, target, flag, parameter);
    }
}