using Irc.Interfaces;
using Irc.Security;

namespace Irc.Extensions.Security.Credentials;

public class NTLMCredentials : NtlmProvider, ICredentialProvider
{
    private readonly Dictionary<string, Credential> _credentials = new();

    public NTLMCredentials(Dictionary<string, Credential> credentials)
    {
        _credentials = credentials;
    }

    public ICredential ValidateTokens(Dictionary<string, string> tokens)
    {
        throw new NotImplementedException();
    }

    public ICredential? GetUserCredentials(string domain, string username)
    {
        _credentials.TryGetValue($"{domain}\\{username}", out var credential);
        return credential;
    }
}