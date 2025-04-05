using Irc.Interfaces;
using Irc.Security;

namespace Irc.Extensions.Security.Credentials;

public class DefaultProvider: ICredentialProvider
{
    public ICredential? ValidateTokens(Dictionary<string, string> tokens)
    {
        return null;
    }

    public ICredential? GetUserCredentials(string domain, string username)
    {
        return null;
    }
}