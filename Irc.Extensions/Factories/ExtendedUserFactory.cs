using Irc.Enumerations;
using Irc.Extensions.Objects.User;
using Irc.Factories;
using Irc.IO;
using Irc.Objects;
using Irc.Objects.Server;
using Irc7d;

namespace Irc.Extensions.Factories;

public class ExtendedUserFactory : IUserFactory
{
    public IUser Create(IServer server, IConnection connection)
    {
        var nominatedProtocol = server.GetProtocol(EnumProtocolType.IRC);
        if (nominatedProtocol == null) throw new Exception("No IRC protocol found");
        
        return new ExtendedUser(connection, nominatedProtocol,
            new DataRegulator(server.MaxInputBytes, server.MaxOutputBytes),
            new FloodProtectionProfile(), new DataStore(connection.GetId().ToString(), "store"), new ExtendedUserModes(),
            server);
    }
}