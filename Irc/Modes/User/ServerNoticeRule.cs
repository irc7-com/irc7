using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Modes.User;

public class ServerNoticeRule : ModeRule, IModeRule
{
    public ServerNoticeRule() : base(Resources.UserModeServerNotice)
    {
    }

    public new EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        return EnumIrcError.OK;
    }
}