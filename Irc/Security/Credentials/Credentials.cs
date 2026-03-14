using Irc.Interfaces;

namespace Irc.Security.Credentials;

public class Credentials : ICredentialProvider
{
    private readonly Dictionary<string, Credential> _credentials;

    public Credentials(Dictionary<string, Credential> credentials)
    {
        _credentials = credentials;
    }

    public new ICredential ValidateTokens(Dictionary<string, string> tokens)
    {
        throw new NotImplementedException();
    }

    public new ICredential? GetUserCredentials(string domain, string username)
    {
        _credentials.TryGetValue($"{domain}\\{username}", out var credential);
        return credential;
    }
}