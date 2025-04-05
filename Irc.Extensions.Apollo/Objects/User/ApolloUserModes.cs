using Irc.Extensions.Objects.User;

namespace Irc.Extensions.Apollo.Objects.User;

public class ApolloUserModes : ExtendedUserModes
{
    public ApolloUserModes()
    {
        Modes.Add(ApolloResources.UserModeHost, new Modes.User.Host());
    }
}