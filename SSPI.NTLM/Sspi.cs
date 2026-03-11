using System.Runtime.InteropServices;

namespace SSPI.NTLM;

/// <summary>
/// P/Invoke declarations for the Devolutions SSPI Rust library.
///
/// This could probably be named better and moved to a more appropriate location, but for now it's here.
/// This also could be programmed better but I coded this in a hack-ing way to get it working.
///
/// @author: Ricardo de Vries <ricardozegt@gmail.com>
/// </summary>
public static partial class Sspi
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

    // SecBufferDesc version
    public const uint SECBUFFER_VERSION = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct SecHandle
    {
        public IntPtr dwLower;
        public IntPtr dwUpper;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SecBuffer
    {
        public uint cbBuffer;
        public uint BufferType;
        public IntPtr pvBuffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SecBufferDesc
    {
        public uint ulVersion;
        public uint cBuffers;
        public IntPtr pBuffers;
    }

    public static SecBufferDesc CreateSecBufferDesc(ref SecBuffer buffer)
    {
        return new SecBufferDesc
        {
            ulVersion = SECBUFFER_VERSION,
            cBuffers = 1,
            pBuffers = IntPtr.Zero
        };
    }

    [DllImport("DevolutionsSspi", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int AcquireCredentialsHandleW(
        string? pszPrincipal,
        string pszPackage,
        uint fCredentialUse,
        IntPtr pvLogonId,
        IntPtr pAuthData,
        IntPtr pGetKeyFn,
        IntPtr pvGetKeyArgument,
        ref SecHandle phCredential,
        out long ptsExpiry);

    [DllImport("DevolutionsSspi", SetLastError = true)]
    public static extern int AcceptSecurityContext(
        ref SecHandle phCredential,
        IntPtr phContext,
        ref SecBufferDesc pInput,
        uint fContextReq,
        uint TargetDataRep,
        ref SecHandle phNewContext,
        ref SecBufferDesc pOutput,
        out uint pfContextAttr,
        out long ptsTimeStamp);

    [DllImport("DevolutionsSspi", SetLastError = true)]
    public static extern int FreeCredentialsHandle(ref SecHandle phCredential);

    [DllImport("DevolutionsSspi", SetLastError = true)]
    public static extern int DeleteSecurityContext(ref SecHandle phContext);

    [DllImport("DevolutionsSspi", SetLastError = true)]
    public static extern int FreeContextBuffer(IntPtr pvContextBuffer);

    [DllImport("DevolutionsSspi", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int SspiEncodeStringsAsAuthIdentity(
        string pszUserName,
        string? pszDomainName,
        string pszPackedCredentialsString,
        out IntPtr ppAuthIdentity);

    [DllImport("DevolutionsSspi", SetLastError = true)]
    public static extern int SspiFreeAuthIdentity(IntPtr pAuthIdentity);
}
