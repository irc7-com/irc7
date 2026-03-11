using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.User;

namespace Irc.Modes.User;

public class BotRule : ModeRule, IModeRule
{
    public BotRule() : base(Resources.UserModeBot)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        if (source == target)
        {
            var userModes = (UserModes)target.Modes;
            userModes.Bot.ModeValue = flag;
            DispatchModeChange(source, target, flag, parameter);
            return EnumIrcError.OK;
        }

        return EnumIrcError.ERR_NOSUCHCHANNEL;
    }
}
