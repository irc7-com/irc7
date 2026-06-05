using Irc.Enumerations;

namespace Irc.Security;

public sealed class PermissionProfile
{
    public List<string> Protocols { get; set; } = new();
    public string Modes { get; set; } = string.Empty;
    public bool Guest { get; set; }
    public EnumUserAccessLevel Level { get; set; }
    public string Prefix { get; set; } = string.Empty;
}

