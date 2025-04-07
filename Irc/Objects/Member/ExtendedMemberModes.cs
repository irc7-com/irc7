using Irc.Interfaces;
using Irc.Modes.Channel.Member;

namespace Irc.Objects.Member;

public class ExtendedMemberModes : Member, IMemberModes
{
    public ExtendedMemberModes(IUser User) : base(User)
    {
        Modes.Add(ExtendedResources.MemberModeOwner, new Owner());
    }

    public new string GetListedMode()
    {
        if (IsOwner()) return ExtendedResources.MemberModeFlagOwner.ToString();

        return base.GetListedMode();
    }

    public new char GetModeChar()
    {
        if (IsOwner()) return ExtendedResources.MemberModeOwner;

        return base.GetModeChar();
    }

    public new bool IsNormal()
    {
        return !IsOwner() && base.IsNormal();
    }

    public new void SetNormal()
    {
        SetOwner(false);
        base.SetNormal();
    }

    public new bool IsOwner()
    {
        return GetModeChar(ExtendedResources.MemberModeOwner) > 0;
    }

    public new void SetOwner(bool flag)
    {
        Modes[ExtendedResources.MemberModeOwner].Set(flag ? 1 : 0);
    }
}