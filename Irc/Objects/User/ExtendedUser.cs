using Irc.Access.User;
using Irc.Interfaces;

namespace Irc.Objects.User;

public class ExtendedUser : User, IExtendedChatObject
{
    private readonly UserAccess _accessList = new();
    protected UserPropCollection _properties;

    public ExtendedUser(IConnection connection, IProtocol protocol, IDataRegulator dataRegulator,
        IFloodProtectionProfile floodProtectionProfile, IDataStore dataStore, IModeCollection modes, IServer server) :
        base(connection, protocol, dataRegulator, floodProtectionProfile, dataStore, modes, server)
    {
        _properties = new UserPropCollection(dataStore);
    }

    public IPropCollection PropCollection => _properties;

    public IAccessList AccessList => _accessList;
}