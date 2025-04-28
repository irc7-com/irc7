using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc.Modes;

public class ModeRule : IModeRule
{
    public ModeRule(char modeChar, bool requiresParameter = false, int initialValue = 0)
    {
        ModeChar = modeChar;
        Value = initialValue;
        RequiresParameter = requiresParameter;
    }

    protected char ModeChar { get; }

    public bool ModeValue
    {
        get
        {
            // Necessary as this is a mode rule, we need to check if the value is greater than 0
            // E.g. UserLimit could be > 1
            return Value > 0;
        }
        set
        {
            Value = Convert.ToInt32(value);
        }
    }

    public int Value { get; set; }
    public bool RequiresParameter { get; }

    // Although the below is a string we are to evaluate and cast to integer
    // We can also throw bad value here if it is not the desired type
    public EnumIrcError Evaluate(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        throw new NotSupportedException();
    }

    public void DispatchModeChange(IChatObject source, IChatObject target, bool flag, string parameter)
    {
        DispatchModeChange(ModeChar, source, target, flag, parameter);
    }

    public void Set(int value)
    {
        Value = value;
    }

    public void Set(bool value)
    {
        Value = value ? 1 : 0;
    }

    public int Get()
    {
        return Value;
    }

    public char GetModeChar()
    {
        return ModeChar;
    }

    public static void DispatchModeChange(char modeChar, IChatObject source, IChatObject target, bool flag,
        string parameter)
    {
        target.Send(
            Raws.RPL_MODE_IRC(
                (IUser)source,
                target,
                $"{(flag ? "+" : "-")}{modeChar}{(parameter != null ? $" {parameter}" : string.Empty)}"
            )
        );
    }

    public static void DispatchModeChange(ChatObject recipientObject, char modeChar, ChatObject source,
        ChatObject target,
        bool flag, string parameter)
    {
        recipientObject.Send(
            Raws.RPL_MODE_IRC(
                (IUser)source,
                target,
                $"{(flag ? "+" : "-")}{modeChar}{(parameter != null ? $" {parameter}" : string.Empty)}"
            )
        );
    }
}