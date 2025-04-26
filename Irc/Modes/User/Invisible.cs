using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.User;

namespace Irc.Modes.User;

public class Invisible : ModeRule, IModeRule
{
    public Invisible() : base(Resources.UserModeInvisible)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        if (source == target)
        {
            var userModes = (UserModes)target.Modes;
            userModes.Invisible.ModeValue = flag;
            DispatchModeChange(source, target, flag, parameter);
            return EnumIrcError.OK;
        }

        return EnumIrcError.ERR_NOSUCHCHANNEL;
    }
}