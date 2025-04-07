using System.Globalization;
using Irc.Interfaces;

namespace Irc.Security.Packages;

public class GateKeeperPassport : GateKeeper
{
    public string Puid = string.Empty;

    public GateKeeperPassport(ICredentialProvider credentialProvider) : base(credentialProvider)
    {
        ServerSequence = EnumSupportPackageSequence.SSP_INIT;
        Guest = false;
        Listed = false;
    }

    public override SupportPackage CreateInstance(ICredentialProvider credentialProvider)
    {
        return new GateKeeperPassport(credentialProvider);
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
            var ticket = ExtractCookie(data);
            if (string.IsNullOrWhiteSpace(ticket)) return EnumSupportPackageSequence.SSP_FAILED;

            var profile = ExtractCookie(data.Substring(8 + ticket.Length));
            if (string.IsNullOrWhiteSpace(profile)) return EnumSupportPackageSequence.SSP_FAILED;

            Credentials = CredentialProvider.ValidateTokens(
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

    private string ExtractCookie(string cookie)
    {
        if (cookie.Length < 8) return string.Empty;

        int.TryParse(cookie.Substring(0, 8), NumberStyles.HexNumber, null, out var cookieLen);

        if (cookie.Length < 8 + cookieLen) return string.Empty;

        return cookie.Substring(8, cookieLen);
    }
}