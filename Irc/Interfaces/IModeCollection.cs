namespace Irc.Interfaces;

public interface IModeCollection
{
    IModeRule this[char mode] { get; }
    void SetModeChar(char mode, int value);
    void ToggleModeChar(char mode, bool flag);
    int GetModeChar(char mode);
    string GetModeString();
    bool HasMode(char mode);
    string GetSupportedModes();
    string ToString();
}