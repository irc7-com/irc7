﻿using Irc.Interfaces;
using Irc.Objects.Collections;
using Irc.Props.User;

namespace Irc.Objects.User;

public class UserProps : PropCollection
{
    private readonly IDataStore _dataStore;

    public UserProps(IServer server, IDataStore dataStore)
    {
        // IRC Props
        AddProp(new OID(dataStore));
        AddProp(new Nick(dataStore));

        // Apollo Props
        AddProp(new SubscriberInfo(server));
        AddProp(new Msnprofile());
        AddProp(new Role(server));
        _dataStore = dataStore;
    }
}