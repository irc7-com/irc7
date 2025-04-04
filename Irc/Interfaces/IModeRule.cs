using Irc.Enumerations;

namespace Irc.Interfaces;

public interface IModeRule
{
    bool RequiresParameter { get; }
    void Set(int value);
    void Set(bool value);
    int Get();
    char GetModeChar();

    EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter);
    void DispatchModeChange(IChatObject source, IChatObject target, bool flag, string parameter);
}