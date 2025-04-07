using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Security;

public class Credential : ICredential
{
    public string Domain { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string UserGroup { get; set; } = string.Empty;
    public string Modes { get; set; } = string.Empty;
    public bool Guest { get; set; }
    public long IssuedAt { get; set; }
    public EnumUserAccessLevel Level { get; set; }

    public string GetDomain()
    {
        return Domain;
    }

    public string GetUsername()
    {
        return Username;
    }

    public string GetPassword()
    {
        return Password;
    }

    public string GetNickname()
    {
        return Nickname;
    }

    public string GetUserGroup()
    {
        return UserGroup;
    }

    public string GetModes()
    {
        return Modes;
    }

    public long GetIssuedAt()
    {
        return IssuedAt;
    }

    public EnumUserAccessLevel GetLevel()
    {
        return Level;
    }
}