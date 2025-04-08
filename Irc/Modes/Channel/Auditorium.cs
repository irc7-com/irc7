using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Modes.Channel;

public class Auditorium : ModeRuleChannel, IModeRule
{
    public Auditorium() : base(Resources.ChannelModeAuditorium)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EvaluateAndSet(source, target, flag, parameter);
    }
}