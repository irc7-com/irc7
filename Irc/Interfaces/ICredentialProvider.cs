using Irc.Security;

namespace Irc.Interfaces;

public interface ICredentialProvider
{
    ICredential? ValidateTokens(Dictionary<string, string> tokens);
    ICredential? GetUserCredentials(string domain, string username);
}