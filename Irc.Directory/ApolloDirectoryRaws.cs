using Irc.Interfaces;

public static class ApolloDirectoryRaws
{
    public static string RPL_FINDS_MSN(DirectoryServer server, IUser user)
    {
        return $":{server} 613 {user} :{server.ChatServerIp} {server.ChatServerPort}";
    }
}