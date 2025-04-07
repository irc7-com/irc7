using Irc.Interfaces;
using Irc.Objects.Server;
using Irc.Props.User;

namespace Irc.Objects.User;

public class ApolloUserPropCollection : UserPropCollection
{
    public ApolloUserPropCollection(ApolloServer apolloServer, IDataStore dataStore) : base(dataStore)
    {
        AddProp(new SubscriberInfo(apolloServer));
        AddProp(new Msnprofile());
        AddProp(new Role(apolloServer));
    }
}