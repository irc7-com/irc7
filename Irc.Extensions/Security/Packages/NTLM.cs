using Irc.Extensions.NTLM;
using Irc.Helpers;
using Irc.Interfaces;
using Irc.Security;

namespace Irc.Extensions.Security.Packages;
// NTLM Implementation by Sky
// Created: Long time ago...
// NTLM is required for the CAC to work

public class NTLM : SupportPackage, ISupportPackage
{
    private readonly ICredentialProvider? _credentialProvider;
    private readonly NTLMShared.TargetInformation _targetInformation = new();
    private ICredential? _credential;
    private NtlmType1Message _message1;
    private NtlmType2Message _message2;
    private NtlmType3Message _message3;

    public NTLM(ICredentialProvider? credentialProvider)
    {
        Listed = true;
        _credentialProvider = credentialProvider;
    }

    public string ServerDomain { get; set; } = "cg";

    public override SupportPackage CreateInstance(ICredentialProvider? credentialProvider)
    {
        return new NTLM(credentialProvider ?? _credentialProvider);
    }

    public ICredential? GetCredentials()
    {
        return _credential;
    }

    public override EnumSupportPackageSequence InitializeSecurityContext(string data, string ip)
    {
        try
        {
            _message1 = new NtlmType1Message(data);

            var isOEM = !_message1.EnumeratedFlags[NtlmFlag.NTLMSSP_NEGOTIATE_UNICODE];

            _targetInformation.DomainName = isOEM ? "DOMAIN" : "DOMAIN".ToUnicodeString();
            _targetInformation.ServerName = isOEM ? "TK2CHATCHATA01" : "TK2CHATCHATA01".ToUnicodeString();
            _targetInformation.DnsDomainName =
                isOEM ? "TK2CHATCHATA01.Microsoft.Com" : "TK2CHATCHATA01.Microsoft.Com".ToUnicodeString();
            _targetInformation.DnsServerName =
                isOEM ? "TK2CHATCHATA01.Microsoft.Com" : "TK2CHATCHATA01.Microsoft.Com".ToUnicodeString();

            return EnumSupportPackageSequence.SSP_OK;
        }
        catch (Exception)
        {
            return EnumSupportPackageSequence.SSP_FAILED;
        }
    }

    public override string CreateSecurityChallenge()
    {
        try
        {
            _message2 = new NtlmType2Message(_message1.Flags, _targetInformation.DomainName, _targetInformation);
            return _message2.ToString() ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public override EnumSupportPackageSequence AcceptSecurityContext(string data, string ip)
    {
        if (_credentialProvider == null) return EnumSupportPackageSequence.SSP_FAILED;

        try
        {
            _message3 = new NtlmType3Message(data);

            _credential = _credentialProvider.GetUserCredentials(_message3.TargetName, _message3.UserName);

            if (_credential != null)
                if (_message3.VerifySecurityContext(_message2.Challenge.ToAsciiString(), _credential.GetPassword()))
                {
                    Authenticated = true;
                    return EnumSupportPackageSequence.SSP_OK;
                }

            return EnumSupportPackageSequence.SSP_FAILED;
        }
        catch (Exception)
        {
            return EnumSupportPackageSequence.SSP_FAILED;
        }
    }

    public override string GetDomain()
    {
        return ServerDomain;
    }

    public override string GetPackageName()
    {
        return nameof(NTLM);
    }
}