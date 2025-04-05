using System;
using Irc.Extensions.Apollo.Objects.User;
using Irc.Interfaces;
using Irc.Objects;
using Irc.Objects.Server;

namespace Irc.Extensions.Apollo.Directory
{
    public static class ApolloDirectoryRaws
    {
        public static string RPL_FINDS_MSN(DirectoryServer server, IUser user)
        {
            return $":{server} 613 {user} :{server.ChatServerIp} {server.ChatServerPort}";
        }
    }
}

