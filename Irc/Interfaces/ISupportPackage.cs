using Irc.Enumerations;
using Irc.Security.Credentials;

namespace Irc.Interfaces;

public interface ISaslHandler
{
    string GetAuthResponse();
    EnumSupportPackageSequence InitializeSecurityContext(string package, string token, string ip);
    EnumSupportPackageSequence AcceptSecurityContext(string package, string token, string ip);
    string GetDomain();
    string GetPackageName();
    ICredential? GetCredentials();
    void SetCredentials(ICredential? credentials);
    bool IsAuthenticated();
    string[] SupportedPackages { get; }
    bool RequiresPassport { get; set; }
    bool PendingPassportCreds { get; set; }
    PassportProvider PassportProvider { get; set; }
}