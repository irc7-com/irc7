using Irc.Modes.User;

namespace Irc.Interfaces;

public interface IUserModes: IModeCollection
{
    Oper Oper { get; }
    Invisible Invisible { get; }
    Secure Secure { get; }
    Admin Admin { get; }
    Isircx Isircx { get; }
    Gag Gag { get; }
    Host Host { get; }
    string ToString();
    void SetModeValue(char mode, int value);
    void SetModeValue(char mode, bool value);
    void ToggleModeValue(char mode, bool flag);
    bool IsEnabled(char mode);
    int GetModeValue(char mode);
    string GetModeString();
    IModeRule this[char mode] { get; set; }
    string GetSupportedModes();
    bool HasMode(char mode);
}