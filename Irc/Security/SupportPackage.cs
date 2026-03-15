using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;
using Vortex.Sspi;

namespace Irc.Security;

public class SupportPackage : ISupportPackage
{
    // Credential use flags
    public const uint SECPKG_CRED_INBOUND = 1;

    // Context requirement flags
    public const uint ASC_REQ_CONNECTION = 0x00000800;
    public const uint ASC_REQ_ALLOCATE_MEMORY = 0x00000100;

    // Data representation
    public const uint SECURITY_NATIVE_DREP = 0x00000010;

    // Buffer types
    public const uint SECBUFFER_TOKEN = 2;

    // Status codes
    public const int SEC_E_OK = 0;
    public const int SEC_I_CONTINUE_NEEDED = 0x00090312;
    public const int SEC_I_COMPLETE_NEEDED = 0x00090313;
    public const int SEC_I_COMPLETE_AND_CONTINUE = 0x00090314;
    
    protected ICredential? Credentials;
    private readonly ICredentialProvider CredentialsProvider;
    public bool Guest;
    public bool Listed = true;
    public EnumSupportPackageSequence ServerSequence;
    private SspiSession _sspiSession;

    public uint ServerVersion;
    public bool Authenticated { get; protected set; }

    public SupportPackage(ICredentialProvider credentialProvider)
    {
        CredentialsProvider = credentialProvider;
        _sspiSession = new SspiSession();
    }
    
    public virtual SupportPackage CreateInstance(ICredentialProvider credentialProvider)
    {
        return new SupportPackage(credentialProvider);
    }

    public virtual EnumSupportPackageSequence InitializeSecurityContext(string token, string ip,
        out byte[]? responseToken)
    {
        responseToken = _sspiSession.ParseToken(token.ToByteArray(), out var result);
        if (result != SEC_I_CONTINUE_NEEDED)
        {
            return EnumSupportPackageSequence.SSP_FAILED;
        } 
        
        return EnumSupportPackageSequence.SSP_OK;
    }

    public virtual EnumSupportPackageSequence AcceptSecurityContext(string token, string ip, out byte[] responseToken)
    {
        responseToken = _sspiSession.ParseToken(token.ToByteArray(), out var result);

        if (result != SEC_E_OK)
        {
            _sspiSession.Dispose();
            _sspiSession = null;
            return EnumSupportPackageSequence.SSP_FAILED;
        } 
        
        var id = _sspiSession.GetIdentity();
        Credentials = CredentialsProvider.GetUserCredentials(id.Domain, id.Username);

        if (string.IsNullOrEmpty(Credentials.Password) ||
            Credentials.Password.Length % 2 != 0 ||
            !System.Text.RegularExpressions.Regex.IsMatch(Credentials.Password, @"^[0-9A-Fa-f]+$"))
        {
            throw new InvalidOperationException(
                $"Credential password for '{id.Username}' is not a valid hex string. Use --createPassword to generate one.");
        }

        var hash1 = Convert.FromHexString(Credentials.Password);

        var verifyResult = _sspiSession.Verify(hash1);
        if (verifyResult == SEC_E_OK) Authenticated = true;
        
        _sspiSession.Dispose();
        _sspiSession = null;
        return verifyResult == SEC_E_OK ? EnumSupportPackageSequence.SSP_OK : EnumSupportPackageSequence.SSP_FAILED;
    }

    public virtual string GetDomain()
    {
        return Credentials.Domain;
    }

    public virtual string GetPackageName()
    {
        return "NTLM";
    }

    public virtual ICredential? GetCredentials()
    {
        return Credentials;
    }

    public bool IsAuthenticated()
    {
        return Authenticated;
    }
}