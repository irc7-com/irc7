namespace Irc.Interfaces;

public interface IModeCollection
{
    void SetModeChar(char mode, int value);
    void ToggleModeChar(char mode, bool flag);
    int GetModeChar(char mode);
    string GetModeString();
    IModeRule this[char mode] { get; }
    bool HasMode(char mode);
    string GetSupportedModes();
    string ToString();
}