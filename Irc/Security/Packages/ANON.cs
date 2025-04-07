using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Security.Packages;

public class ANON : SupportPackage
{
    public ANON()
    {
        Guest = true;
        Authenticated = true;
        Listed = true;
    }

    public new EnumSupportPackageSequence InitializeSecurityContext(string data, string ip)
    {
        return EnumSupportPackageSequence.SSP_AUTHENTICATED;
    }

    public new EnumSupportPackageSequence AcceptSecurityContext(string data, string ip)
    {
        return EnumSupportPackageSequence.SSP_AUTHENTICATED;
    }

    public new string GetDomain()
    {
        return nameof(ANON);
    }

    public new string GetPackageName()
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

    public new SupportPackage CreateInstance(ICredentialProvider credentialProvider)
    {
        return new ANON();
    }

    public string CreateSecurityChallenge(EnumSupportPackageSequence stage)
    {
        return string.Empty;
    }
}