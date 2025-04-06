using Irc.Enumerations;
using Irc.Extensions.Apollo.Objects.User;
using Irc.Factories;
using Irc.IO;
using Irc.Objects;
using Irc.Objects.Server;
using Irc7d;

namespace Irc.Extensions.Apollo.Factories;

public class ApolloUserFactory : IUserFactory
{
    public IUser Create(IServer server, IConnection connection)
    {
        var protocol = server.GetProtocol(EnumProtocolType.IRC);
        if (protocol == null) throw new Exception("No IRC protocol");
        
        return new ApolloUser(connection, protocol,
            new DataRegulator(server.MaxInputBytes, server.MaxOutputBytes),
            new FloodProtectionProfile(), new DataStore(connection.GetId().ToString(), "store"), new ApolloUserModes(),
            server);
    }
}