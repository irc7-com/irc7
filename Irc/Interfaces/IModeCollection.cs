namespace Irc.Interfaces;

public interface IModeCollection
{
    public IModeRule this[char mode] { get; }
    public void SetModeValue(char mode, int value);
    public void ToggleModeValue(char mode, bool flag);
    public int GetModeValue(char mode);
    public string GetModeString();
    public bool HasMode(char mode);
    public string GetSupportedModes();
    public string ToString();
}