using Irc.Enumerations;
using Irc.Security;

namespace Irc.Extensions.Security.Packages;

public class ANON : SupportPackage
{
    public ANON()
    {
        Guest = true;
        Authenticated = true;
        Listed = true;
    }

    public EnumSupportPackageSequence InitializeSecurityContext(string data, string ip)
    {
        return EnumSupportPackageSequence.SSP_AUTHENTICATED;
    }

    public EnumSupportPackageSequence AcceptSecurityContext(string data, string ip)
    {
        return EnumSupportPackageSequence.SSP_AUTHENTICATED;
    }

    public string GetDomain()
    {
        return nameof(ANON);
    }

    public string GetPackageName()
    {
        return nameof(ANON);
    }

    public override ICredential? GetCredentials()
    {
        return new Credential
        {
            Level = EnumUserAccessLevel.Member,
            Domain = GetType().Name,
            Username = string.Empty,
            Guest = true
        };
    }

    public SupportPackage CreateInstance(ICredentialProvider credentialProvider)
    {
        return new ANON();
    }

    public string CreateSecurityChallenge(EnumSupportPackageSequence stage)
    {
        return string.Empty;
    }
}