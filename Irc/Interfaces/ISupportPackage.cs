using Irc.Enumerations;
using Irc.Security;

namespace Irc.Interfaces;

public interface ISupportPackage
{
    SupportPackage CreateInstance(ICredentialProvider credentialProvider);
    EnumSupportPackageSequence InitializeSecurityContext(string token, string ip, out byte[]? responseToken);
    EnumSupportPackageSequence AcceptSecurityContext(string token, string ip, out byte[]? responseToken);
    string GetDomain();
    string GetPackageName();
    ICredential? GetCredentials();
    bool IsAuthenticated();
}