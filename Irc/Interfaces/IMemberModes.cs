using Irc.Modes.Channel.Member;

namespace Irc.Interfaces;

public interface IMemberModes
{
    string GetModeString();
    string GetListedMode();
    char GetModeChar();
    bool HasModes();
    void ResetModes();
    OwnerRule Owner { get; }
    OperatorRule Operator { get; }
    VoiceRule Voice { get; }
}