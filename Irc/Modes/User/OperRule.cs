using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.User;

namespace Irc.Modes.User;

public class OperRule : ModeRule, IModeRule
{
    public OperRule() : base(Resources.UserModeOper)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        // :sky-8a15b323126 908 Sky :No permissions to perform command
        if (source is IUser && ((IUser)source).IsSysop() && flag == false)
        {
            var userModes = (UserModes)target.Modes;
            userModes.Oper.ModeValue = flag;
            DispatchModeChange(source, target, flag, parameter);
            return EnumIrcError.OK;
        }

        return EnumIrcError.ERR_NOPERMS;
    }
}