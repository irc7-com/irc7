using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Modes.Channel;

public class Subscriber : ModeRuleChannel, IModeRule
{
    public Subscriber() : base(ApolloResources.ChannelModeSubscriber)
    {
    }

    public EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EvaluateAndSet(source, target, flag, parameter);
    }
}