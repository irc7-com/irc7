using Irc.Constants;
using Irc.Interfaces;

namespace Irc.Objects.Member;

public class ExtendedMember : ExtendedMemberModes, IChannelMember
{
    public ExtendedMember(IUser user) : base(user)
    {
    }

    public new void SetOwner(bool flag)
    {
        Modes[Resources.MemberModeOwner].Set(flag ? 1 : 0);
    }
}