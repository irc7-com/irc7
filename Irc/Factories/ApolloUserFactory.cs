using Irc.Enumerations;
using Irc.Interfaces;
using Irc.IO;
using Irc.Objects.User;

namespace Irc.Factories;

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