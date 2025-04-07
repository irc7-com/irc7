using Irc.Interfaces;
using Irc.Objects.Collections;
using Irc.Props.User;

namespace Irc.Objects.User;

public class UserPropCollection : PropCollection
{
    private readonly IDataStore dataStore;

    public UserPropCollection(IDataStore dataStore)
    {
        AddProp(new OID(dataStore));
        AddProp(new Nick(dataStore));
        this.dataStore = dataStore;
    }
}