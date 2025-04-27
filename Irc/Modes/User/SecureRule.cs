using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Modes.User;

public class SecureRule : ModeRule, IModeRule
{
    public SecureRule() : base(Resources.UserModeSecure)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EnumIrcError.ERR_NOPERMS;
    }
}