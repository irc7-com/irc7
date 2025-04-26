using Irc.Enumerations;

namespace Irc.Interfaces;

public interface IPropRule
{
    EnumChannelAccessLevel ReadAccessLevel { get; }
    EnumChannelAccessLevel WriteAccessLevel { get; }
    string Name { get; }
    string Value { get; set; }
    bool ReadOnly { get; }
    EnumIrcError EvaluateSet(IChatObject source, IChatObject target, string propValue);
    EnumIrcError EvaluateGet(IChatObject source, IChatObject target);
    string GetValue(IChatObject target);
    void SetValue(string value);
}