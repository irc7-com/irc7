using Irc.Interfaces;

namespace Irc.Objects.Collections;

public class ModeCollection : IModeCollection
{
    protected Dictionary<char, IModeRule> Modes = new();
    // TODO: <CHANKEY> Below is temporary until implemented properly
    protected string? Keypass = string.Empty;

    public void SetModeChar(char mode, int value)
    {
        if (Modes.ContainsKey(mode)) Modes[mode].Set(value);
    }

    public void ToggleModeChar(char mode, bool flag)
    {
        SetModeChar(mode, flag ? 1 : 0);
    }

    public int GetModeChar(char mode)
    {
        Modes.TryGetValue(mode, out var value);
        return value?.Get() ?? 0;
    }

    public string GetModeString() {
        return $"{new string(Modes.Where(mode => mode.Value.Get() > 0).Select(mode => mode.Key).ToArray())}";
    }

    public IModeRule this[char mode]
    {
        get => Modes[mode];
        set => Modes[mode] = value;
    }

    public string GetSupportedModes()
    {
        return new(Modes.Keys.OrderBy(x => x).ToArray());
    }

    public bool HasMode(char mode)
    {
        return Modes.Keys.Contains(mode);
    }

    public override string ToString()
    {
        return $"+{new string(Modes.Where(mode => mode.Value.Get() > 0).Select(mode => mode.Key).ToArray())}";
    }
}