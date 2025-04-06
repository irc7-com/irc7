using Irc.Extensions.Security;
using Irc.Interfaces;

namespace Irc.Security;

public abstract class CredentialProvider : ICredentialProvider
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