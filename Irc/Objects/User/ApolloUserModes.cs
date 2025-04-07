using Irc.Modes.User;

namespace Irc.Objects.User;

public class ApolloUserModes : ExtendedUserModes
{
    public ApolloUserModes()
    {
        Modes.Add(ApolloResources.UserModeHost, new Host());
    }
}