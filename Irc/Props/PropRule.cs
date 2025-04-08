using System.Text.RegularExpressions;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Props;

public class PropRule : IPropRule
{
    private readonly string validationMask;

    public PropRule(string name, EnumChannelAccessLevel readAccessLevel, EnumChannelAccessLevel writeAccessLevel,
        string validationMask, string initialValue, bool readOnly = false)
    {
        Name = name;
        ReadAccessLevel = readAccessLevel;
        WriteAccessLevel = writeAccessLevel;
        this.validationMask = validationMask;
        _value = initialValue;
        ReadOnly = readOnly;
    }

    private string _value { get; set; }

    public string Name { get; }

    // TODO: Figure out how to refactor to also accommodate User props access levels
    public EnumChannelAccessLevel ReadAccessLevel { get; }
    public EnumChannelAccessLevel WriteAccessLevel { get; }
    public bool ReadOnly { get; }

    public virtual EnumIrcError EvaluateSet(IChatObject source, IChatObject target, string propValue)
    {
        if (target is IChannel)
        {
            var channel = (IChannel)target;
            var member = channel.GetMember((IUser)source);

            if (member == null) return EnumIrcError.ERR_NOPERMS;

            if (member.GetLevel() < WriteAccessLevel) return EnumIrcError.ERR_NOPERMS;
        }
        else if (WriteAccessLevel == EnumChannelAccessLevel.None || (target is IUser && source != target))
        {
            return EnumIrcError.ERR_NOPERMS;
        }

        // Otherwise perms are OK, it is the same user, or is a server
        var regEx = new Regex(validationMask);
        var match = regEx.Match(propValue);
        if (!match.Success || match.Value.Length != propValue.Length) return EnumIrcError.ERR_BADVALUE;

        return EnumIrcError.OK;
    }

    public virtual EnumIrcError EvaluateGet(IChatObject source, IChatObject target)
    {
        if (target is IChannel)
        {
            var channel = (IChannel)target;
            var member = channel.GetMember((IUser)source);

            if (member == null) return EnumIrcError.ERR_NOPERMS;

            if (member.GetLevel() < ReadAccessLevel) return EnumIrcError.ERR_NOPERMS;
        }
        else if (ReadAccessLevel == EnumChannelAccessLevel.None || (target is IUser && source != target))
        {
            return EnumIrcError.ERR_NOPERMS;
        }

        // Otherwise perms are OK, it is the same user, or is a server
        return EnumIrcError.OK;
    }

    public virtual void SetValue(string value)
    {
        _value = value;
    }

    public virtual string GetValue(IChatObject target)
    {
        return _value;
    }
}