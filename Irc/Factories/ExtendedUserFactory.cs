using Irc.Enumerations;
using Irc.Interfaces;
using Irc.IO;
using Irc.Objects.User;

namespace Irc.Factories;

public class ExtendedUserFactory : IUserFactory
{
    public IUser Create(IServer server, IConnection connection)
    {
        var nominatedProtocol = server.GetProtocol(EnumProtocolType.IRC);
        if (nominatedProtocol == null) throw new Exception("No IRC protocol found");

        return new ExtendedUser(connection, nominatedProtocol,
            new DataRegulator(server.MaxInputBytes, server.MaxOutputBytes),
            new FloodProtectionProfile(), new DataStore(connection.GetId().ToString(), "store"),
            new ExtendedUserModes(),
            server);
    }
}