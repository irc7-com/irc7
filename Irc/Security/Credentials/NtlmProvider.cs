using Irc.Interfaces;

namespace Irc.Security.Credentials;

public class NtlmProvider : ICredentialProvider
{
    public ICredential ValidateTokens(Dictionary<string, string> tokens)
    {
        throw new NotImplementedException();
    }

    public ICredential GetUserCredentials(string domain, string username)
    {
        throw new NotImplementedException();
    }
}