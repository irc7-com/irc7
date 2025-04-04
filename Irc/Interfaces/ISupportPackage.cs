using Irc.Extensions.Security;
using Irc.Security;

namespace Irc.Interfaces;

public interface ISupportPackage
{
    SupportPackage CreateInstance(ICredentialProvider? credentialProvider);
    string CreateSecurityChallenge();
    EnumSupportPackageSequence InitializeSecurityContext(string token, string ip);
    EnumSupportPackageSequence AcceptSecurityContext(string token, string ip);
    string GetDomain();
    string GetPackageName();
    ICredential GetCredentials();
    bool IsAuthenticated();
    void SetChallenge(byte[] new_challenge);
}