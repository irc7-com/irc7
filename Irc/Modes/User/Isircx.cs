using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Modes.User;

public class Isircx : ModeRule, IModeRule
{
    public Isircx() : base(Resources.UserModeIrcx)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EnumIrcError.ERR_UNKNOWNMODEFLAG;
    }
}