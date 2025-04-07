using Irc.Constants;
using Irc.Interfaces;
using Irc.Modes.Channel.Member;
using Irc.Objects.Collections;

namespace Irc.Objects;

public class MemberModes : ModeCollection, IMemberModes
{
    public MemberModes()
    {
        Modes.Add(Resources.MemberModeHost, new Operator());
        Modes.Add(Resources.MemberModeVoice, new Voice());
    }

    public string GetListedMode()
    {
        if (IsOwner()) return Resources.MemberModeFlagOwner.ToString();
        if (IsHost()) return Resources.MemberModeFlagHost.ToString();
        if (IsVoice()) return Resources.MemberModeFlagVoice.ToString();

        return "";
    }

    public char GetModeChar()
    {
        if (IsOwner()) return Resources.MemberModeOwner;
        if (IsHost()) return Resources.MemberModeHost;
        if (IsVoice()) return Resources.MemberModeVoice;

        return (char)0;
    }

    public bool IsOwner()
    {
        // TODO: Need to think about a better way of handling the below
        return Modes.ContainsKey(Resources.MemberModeOwner) && GetModeChar(Resources.MemberModeOwner) > 0;
    }

    public bool IsHost()
    {
        return GetModeChar(Resources.MemberModeHost) > 0;
    }

    public bool IsVoice()
    {
        return GetModeChar(Resources.MemberModeVoice) > 0;
    }

    public bool IsNormal()
    {
        return !IsHost() && !IsVoice();
    }

    public void SetHost(bool flag)
    {
        Modes[Resources.MemberModeHost].Set(flag ? 1 : 0);
    }

    public void SetVoice(bool flag)
    {
        Modes[Resources.MemberModeVoice].Set(flag ? 1 : 0);
    }

    public void SetNormal()
    {
        SetHost(false);
        SetVoice(false);
    }

    public void SetOwner(bool flag)
    {
        throw new NotImplementedException();
    }
}