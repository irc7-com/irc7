using Irc.Interfaces;

namespace Irc.Directory;

public static class DirectoryRaws
{
    public static string RPL_FINDS_MSN(DirectoryServer server, IUser user, string ip, string port)
    {
        return $":{server} 613 {user} :{ip} {port}";
    }
}