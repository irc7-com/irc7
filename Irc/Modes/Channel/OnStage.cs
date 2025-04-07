using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Modes.Channel;

public class OnStage : ModeRuleChannel, IModeRule
{
    public OnStage() : base(ApolloResources.ChannelModeOnStage)
    {
    }

    public EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EvaluateAndSet(source, target, flag, parameter);
    }
}