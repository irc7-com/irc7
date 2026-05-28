using Irc.Enumerations;

namespace Irc.Interfaces;

public interface ISaslHandler
{
    string GetAuthResponse();
    EnumSupportPackageSequence InitializeSecurityContext(string package, string token, string ip);
    EnumSupportPackageSequence AcceptSecurityContext(string package, string token, string ip);
    string GetDomain();
    string GetPackageName();
    ICredential? GetCredentials();
    bool IsAuthenticated();
    string[] SupportedPackages { get; }
}