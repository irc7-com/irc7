using Irc.Extensions.Access.User;
using Irc.Extensions.Interfaces;
using Irc.Interfaces;
using Irc.IO;
using Irc.Objects;
using Irc.Objects.Server;
using Irc7d;

namespace Irc.Extensions.Objects.User;

public class ExtendedUser : global::Irc.Objects.User.User, IExtendedChatObject
{
    protected UserPropCollection _properties;
    private UserAccess _accessList = new();
    public IPropCollection PropCollection => _properties;

    public IAccessList AccessList => _accessList;

    public ExtendedUser(IConnection connection, IProtocol protocol, IDataRegulator dataRegulator,
        IFloodProtectionProfile floodProtectionProfile, IDataStore dataStore, IModeCollection modes, IServer server) :
        base(connection, protocol, dataRegulator, floodProtectionProfile, dataStore, modes, server)
    {
        _properties = new UserPropCollection(dataStore);
    }
}