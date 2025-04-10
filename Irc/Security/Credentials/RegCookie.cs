namespace Irc.Security.Credentials;

public class RegCookie
{
    public long issueDate;
    public string nickname = string.Empty;
    public string salt = string.Empty;
    public int version;

    public RegCookie(int version)
    {
        this.version = version;
    }
}