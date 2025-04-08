using Irc.Interfaces;

namespace Irc.Security.Credentials;

public class DefaultProvider : ICredentialProvider
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