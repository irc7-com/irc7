using Irc.Enumerations;

namespace Irc.Interfaces;

public interface ICredential
{
    public string Domain { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Nickname { get; set; }
    public string UserGroup { get; set; }
    public string Modes { get; set; }
    public bool Guest { get; set; }
    public long IssuedAt { get; set; }
    public EnumUserAccessLevel Level { get; set; }
    EnumUserAccessLevel GetLevel();
    string GetDomain();
    string GetUsername();
    string GetPassword();
    string GetNickname();
    string GetUserGroup();
    string GetModes();
    long GetIssuedAt();
}