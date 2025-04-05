using System.Globalization;
using Irc.Extensions.Security;
using Irc.Security;

namespace Irc.Extensions.Apollo.Security.Packages;

public class NTLMPassport : global::Irc.Extensions.Security.Packages.NTLM
{
    private readonly ICredentialProvider? _credentialProvider;

    public string Puid;

    public NTLMPassport(ICredentialProvider? credentialProvider) : base(credentialProvider)
    {
        _credentialProvider = credentialProvider;
        ServerSequence = EnumSupportPackageSequence.SSP_INIT;
        Guest = false;
        Listed = false;
    }

    public override SupportPackage CreateInstance(ICredentialProvider? credentialProvider)
    {
        return new GateKeeperPassport(_credentialProvider);
    }

    public override EnumSupportPackageSequence AcceptSecurityContext(string data, string ip)
    {
        if (ServerSequence == EnumSupportPackageSequence.SSP_EXT)
        {
            var result = base.AcceptSecurityContext(data, ip);
            if (result != EnumSupportPackageSequence.SSP_OK && result != EnumSupportPackageSequence.SSP_EXT)
                return EnumSupportPackageSequence.SSP_FAILED;

            Authenticated = false;
            ServerSequence = EnumSupportPackageSequence.SSP_CREDENTIALS;
            return EnumSupportPackageSequence.SSP_CREDENTIALS;
        }

        if (ServerSequence == EnumSupportPackageSequence.SSP_CREDENTIALS)
        {
            var ticket = extractCookie(data);
            if (ticket == null) return EnumSupportPackageSequence.SSP_FAILED;

            var profile = extractCookie(data.Substring(8 + ticket.Length));
            if (profile == null) return EnumSupportPackageSequence.SSP_FAILED;

            Credentials = _credentialProvider.ValidateTokens(
                new Dictionary<string, string>
                {
                    { "ticket", ticket },
                    { "profile", profile }
                });

            if (Credentials == null) return EnumSupportPackageSequence.SSP_FAILED;

            Authenticated = true;
            return EnumSupportPackageSequence.SSP_OK;
        }

        return EnumSupportPackageSequence.SSP_FAILED;
    }

    private string extractCookie(string cookie)
    {
        if (cookie.Length < 8) return null;

        int.TryParse(cookie.Substring(0, 8), NumberStyles.HexNumber, null, out var cookieLen);

        if (cookie.Length < 8 + cookieLen) return null;

        return cookie.Substring(8, cookieLen);
    }
}