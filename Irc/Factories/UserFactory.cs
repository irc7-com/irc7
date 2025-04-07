using Irc.Enumerations;
using Irc.Interfaces;
using Irc.IO;
using Irc.Objects.User;

namespace Irc.Factories;

public interface IUserFactory
{
    public IUser Create(IServer server, IConnection connection);
}

public class UserFactory : IUserFactory
{
    public IUser Create(IServer server, IConnection connection)
    {
        var protocol = server.GetProtocol(EnumProtocolType.IRC) ?? new Protocols.Irc();
        return new User(connection, protocol,
            new DataRegulator(server.MaxInputBytes, server.MaxOutputBytes),
            new FloodProtectionProfile(), new DataStore(connection.GetId().ToString(), "store"), new UserModes(),
            server);
    }
}