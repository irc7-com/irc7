using Irc.Interfaces;

namespace Irc.Objects.Collections;

public class ModeCollection : IModeCollection
{
    // TODO: <CHANKEY> Below is temporary until implemented properly
    protected Dictionary<char, IModeRule> Modes = new();

    public void SetModeValue(char mode, int value)
    {
        if (Modes.ContainsKey(mode)) Modes[mode].Set(value);
    }
    
    public void SetModeValue(char mode, bool value)
    {
        if (Modes.ContainsKey(mode)) Modes[mode].Set(value);
    }

    public void ToggleModeValue(char mode, bool flag)
    {
        SetModeValue(mode, flag ? 1 : 0);
    }
    
    public bool IsEnabled(char mode)
    {
        return GetModeValue(mode) == 1;
    }

    public int GetModeValue(char mode)
    {
        Modes.TryGetValue(mode, out var value);
        return value?.Get() ?? 0;
    }

    public string GetModeString()
    {
        return $"{new string(Modes.Where(mode => mode.Value.Get() > 0).Select(mode => mode.Key).ToArray())}";
    }

    public IModeRule this[char mode]
    {
        get => Modes[mode];
        set => Modes[mode] = value;
    }

    public string GetSupportedModes()
    {
        return new string(Modes.Keys.OrderBy(x => x).ToArray());
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