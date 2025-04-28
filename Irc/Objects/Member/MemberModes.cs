using Irc.Constants;
using Irc.Interfaces;
using Irc.Modes.Channel.Member;
using Irc.Objects.Collections;

namespace Irc.Objects.Member;

public class MemberModes : ModeCollection, IMemberModes
{
    public OwnerRule Owner { get; } = new();
    public OperatorRule Operator { get; } = new();
    public VoiceRule Voice { get; } = new();
    public MemberModes()
    {
        Modes.Add(Resources.MemberModeOwner, Owner);
        Modes.Add(Resources.MemberModeHost, Operator);
        Modes.Add(Resources.MemberModeVoice, Voice);
    }

    public string GetListedMode()
    {
        if (Owner.ModeValue) return Resources.MemberModeFlagOwner.ToString();
        if (Operator.ModeValue) return Resources.MemberModeFlagHost.ToString();
        if (Voice.ModeValue) return Resources.MemberModeFlagVoice.ToString();

        return string.Empty;
    }

    public char GetModeChar()
    {
        if (Owner.ModeValue) return Resources.MemberModeOwner;
        if (Operator.ModeValue) return Resources.MemberModeHost;
        if (Voice.ModeValue) return Resources.MemberModeVoice;

        return (char)0;
    }
    
    public void ResetModes()
    {
        Owner.ModeValue = false;
        Operator.ModeValue = false;
        Voice.ModeValue = false;
    }
    
    public bool HasModes() => Owner.ModeValue || Operator.ModeValue || Voice.ModeValue;
}