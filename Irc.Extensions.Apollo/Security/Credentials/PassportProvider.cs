using Irc.Extensions.Apollo.Security.Passport;
using Irc.Interfaces;
using Irc.Security;

namespace Irc.Extensions.Apollo.Security.Credentials;

public class PassportProvider : ICredentialProvider
{
    private readonly PassportV4 _passportV4;

    public PassportProvider(PassportV4 passportV4)
    {
        _passportV4 = passportV4;
    }

    public ICredential GetUserCredentials(string domain, string username)
    {
        throw new NotImplementedException();
    }

    public ICredential? ValidateTokens(Dictionary<string, string> tokens)
    {
        var ticket = tokens["ticket"];
        var profile = tokens["profile"];

        var passportCredentials = _passportV4.ValidateTicketAndProfile(ticket, profile);

        if (passportCredentials == null) return null;

        var credential = new Credential();
        credential.Username = passportCredentials.PUID;
        credential.Domain = passportCredentials.Domain;
        credential.IssuedAt = passportCredentials.IssuedAt;
        return credential;
    }
}