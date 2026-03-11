using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;
using SSPI.NTLM;
using System.Runtime.InteropServices;
using System.Text;

namespace Irc.Security.Packages;

public class NTLM : SupportPackage, ISupportPackage, IDisposable
{
    private readonly ICredentialProvider CredentialsProvider;

    private Sspi.SecHandle _credHandle;
    private Sspi.SecHandle _validationCredHandle;
    private bool _validationCredHandleValid;
    private Sspi.SecHandle _ctxtHandle;
    private bool _ctxtHandleValid;
    private byte[]? _challengeToken;
    private bool _disposed;

    public NTLM(ICredentialProvider credentialProvider)
    {
        Listed = true;
        CredentialsProvider = credentialProvider;
    }

    public string ServerDomain { get; set; } = "CG";

    public override SupportPackage CreateInstance(ICredentialProvider credentialProvider)
    {
        return new NTLM(credentialProvider);
    }

    public override EnumSupportPackageSequence InitializeSecurityContext(string data, string ip)
    {
        try
        {
            byte[] inputBytes = data.ToByteArray();
            int encodeStatus = Sspi.SspiEncodeStringsAsAuthIdentity("placeholder", ServerDomain, "placeholder", out nint pInitialAuthIdentity);

            if (encodeStatus != Sspi.SEC_E_OK)
            {
                return EnumSupportPackageSequence.SSP_FAILED;
            }

            int status;
            try
            {
                status = Sspi.AcquireCredentialsHandleW(
                    null,
                    "NTLM",
                    Sspi.SECPKG_CRED_INBOUND,
                    IntPtr.Zero,
                    pInitialAuthIdentity,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    ref _credHandle,
                    out _);
            }
            finally
            {
                Sspi.SspiFreeAuthIdentity(pInitialAuthIdentity);
            }

            if (status != Sspi.SEC_E_OK)
            {
                return EnumSupportPackageSequence.SSP_FAILED;
            }

            Sspi.SecBuffer inputBuffer = new()
            {
                cbBuffer = (uint)inputBytes.Length,
                BufferType = Sspi.SECBUFFER_TOKEN
            };
            GCHandle inputHandle = GCHandle.Alloc(inputBytes, GCHandleType.Pinned);

            byte[] outputBytes = new byte[16384];
            Sspi.SecBuffer outputBuffer = new()
            {
                cbBuffer = (uint)outputBytes.Length,
                BufferType = Sspi.SECBUFFER_TOKEN
            };
            GCHandle outputHandle = GCHandle.Alloc(outputBytes, GCHandleType.Pinned);

            try
            {
                inputBuffer.pvBuffer = inputHandle.AddrOfPinnedObject();
                outputBuffer.pvBuffer = outputHandle.AddrOfPinnedObject();

                Sspi.SecBufferDesc inputBufferDesc = Sspi.CreateSecBufferDesc(ref inputBuffer);
                Sspi.SecBufferDesc outputBufferDesc = Sspi.CreateSecBufferDesc(ref outputBuffer);

                GCHandle inputDescHandle = GCHandle.Alloc(inputBuffer, GCHandleType.Pinned);
                GCHandle outputDescHandle = GCHandle.Alloc(outputBuffer, GCHandleType.Pinned);

                try
                {
                    inputBufferDesc.pBuffers = inputDescHandle.AddrOfPinnedObject();
                    outputBufferDesc.pBuffers = outputDescHandle.AddrOfPinnedObject();

                    status = Sspi.AcceptSecurityContext(
                        ref _credHandle,
                        IntPtr.Zero, // null context for first call
                        ref inputBufferDesc,
                        Sspi.ASC_REQ_ALLOCATE_MEMORY | Sspi.ASC_REQ_CONNECTION,
                        Sspi.SECURITY_NATIVE_DREP,
                        ref _ctxtHandle,
                        ref outputBufferDesc,
                        out _,
                        out _);

                    outputBuffer = Marshal.PtrToStructure<Sspi.SecBuffer>(outputDescHandle.AddrOfPinnedObject());
                }
                finally
                {
                    inputDescHandle.Free();
                    outputDescHandle.Free();
                }

                if (status == Sspi.SEC_I_CONTINUE_NEEDED || status == Sspi.SEC_E_OK)
                {
                    _ctxtHandleValid = true;

                    _challengeToken = new byte[outputBuffer.cbBuffer];
                    if (outputBuffer.pvBuffer != IntPtr.Zero && outputBuffer.cbBuffer > 0)
                    {
                        Marshal.Copy(outputBuffer.pvBuffer, _challengeToken, 0, (int)outputBuffer.cbBuffer);
                    }

                    return EnumSupportPackageSequence.SSP_OK;
                }

                return EnumSupportPackageSequence.SSP_FAILED;
            }
            finally
            {
                inputHandle.Free();
                outputHandle.Free();
            }
        }
        catch (Exception ex)
        {
            return EnumSupportPackageSequence.SSP_FAILED;
        }
    }

    public override string CreateSecurityChallenge()
    {
        if (_challengeToken == null || _challengeToken.Length == 0)
        {
            return string.Empty;
        }

        return _challengeToken.ToAsciiString();
    }

    public override EnumSupportPackageSequence AcceptSecurityContext(string data, string ip)
    {
        if (CredentialsProvider == null)
        {
            return EnumSupportPackageSequence.SSP_FAILED;
        }

        if (!_ctxtHandleValid)
        {
            return EnumSupportPackageSequence.SSP_FAILED;
        }

        try
        {
            byte[] inputBytes = data.ToByteArray();

            (string? type3Domain, string? username) = ExtractType3Identity(inputBytes);

            if (username == null)
            {
                return EnumSupportPackageSequence.SSP_FAILED;
            }

            string lookupDomain = type3Domain ?? string.Empty;

            Credentials = CredentialsProvider.GetUserCredentials(lookupDomain, username);

            if (Credentials == null && !string.Equals(lookupDomain, ServerDomain, StringComparison.OrdinalIgnoreCase))
            {
                Credentials = CredentialsProvider.GetUserCredentials(ServerDomain, username);
            }

            if (Credentials == null)
            {
                return EnumSupportPackageSequence.SSP_FAILED;
            }

            string password = Credentials.GetPassword();
            int authIdentityStatus = Sspi.SspiEncodeStringsAsAuthIdentity(username, type3Domain, password, out nint pAuthIdentity);

            if (authIdentityStatus != Sspi.SEC_E_OK)
            {
                return EnumSupportPackageSequence.SSP_FAILED;
            }

            try
            {
                int status = Sspi.AcquireCredentialsHandleW(
                    null,
                    "NTLM",
                    Sspi.SECPKG_CRED_INBOUND,
                    IntPtr.Zero,
                    pAuthIdentity,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    ref _validationCredHandle,
                    out _);

                _validationCredHandleValid = status == Sspi.SEC_E_OK;

                if (status != Sspi.SEC_E_OK)
                {
                    return EnumSupportPackageSequence.SSP_FAILED;
                }

                Sspi.SecBuffer inputBuffer = new()
                {
                    cbBuffer = (uint)inputBytes.Length,
                    BufferType = Sspi.SECBUFFER_TOKEN
                };
                GCHandle inputHandle = GCHandle.Alloc(inputBytes, GCHandleType.Pinned);

                byte[] outputBytes = new byte[16384];
                Sspi.SecBuffer outputBuffer = new()
                {
                    cbBuffer = (uint)outputBytes.Length,
                    BufferType = Sspi.SECBUFFER_TOKEN
                };
                GCHandle outputHandle = GCHandle.Alloc(outputBytes, GCHandleType.Pinned);

                try
                {
                    inputBuffer.pvBuffer = inputHandle.AddrOfPinnedObject();
                    outputBuffer.pvBuffer = outputHandle.AddrOfPinnedObject();

                    Sspi.SecBufferDesc inputBufferDesc = Sspi.CreateSecBufferDesc(ref inputBuffer);
                    Sspi.SecBufferDesc outputBufferDesc = Sspi.CreateSecBufferDesc(ref outputBuffer);

                    GCHandle inputDescHandle = GCHandle.Alloc(inputBuffer, GCHandleType.Pinned);
                    GCHandle outputDescHandle = GCHandle.Alloc(outputBuffer, GCHandleType.Pinned);

                    try
                    {
                        inputBufferDesc.pBuffers = inputDescHandle.AddrOfPinnedObject();
                        outputBufferDesc.pBuffers = outputDescHandle.AddrOfPinnedObject();

                        GCHandle ctxtHandlePin = GCHandle.Alloc(_ctxtHandle, GCHandleType.Pinned);
                        try
                        {
                            status = Sspi.AcceptSecurityContext(
                                ref _validationCredHandle,
                                ctxtHandlePin.AddrOfPinnedObject(),
                                ref inputBufferDesc,
                                Sspi.ASC_REQ_CONNECTION,
                                Sspi.SECURITY_NATIVE_DREP,
                                ref _ctxtHandle,
                                ref outputBufferDesc,
                                out _,
                                out _);
                        }
                        finally
                        {
                            ctxtHandlePin.Free();
                        }
                    }
                    finally
                    {
                        inputDescHandle.Free();
                        outputDescHandle.Free();
                    }
                }
                finally
                {
                    inputHandle.Free();
                    outputHandle.Free();
                }

                if (status == Sspi.SEC_E_OK || status == Sspi.SEC_I_COMPLETE_NEEDED || status == Sspi.SEC_I_COMPLETE_AND_CONTINUE)
                {
                    Authenticated = true;

                    return EnumSupportPackageSequence.SSP_OK;
                }

                return EnumSupportPackageSequence.SSP_FAILED;
            }
            finally
            {
                Sspi.SspiFreeAuthIdentity(pAuthIdentity);
            }
        }
        catch (Exception ex)
        {
            return EnumSupportPackageSequence.SSP_FAILED;
        }
    }

    private static (string? domain, string? username) ExtractType3Identity(byte[] data)
    {
        if (data.Length < 52)
        {
            return (null, null);
        }

        if (data[0] != 'N' || data[1] != 'T' || data[2] != 'L' || data[3] != 'M' ||
            data[4] != 'S' || data[5] != 'S' || data[6] != 'P' || data[7] != 0)
        {
            return (null, null);
        }

        uint messageType = BitConverter.ToUInt32(data, 8);
        if (messageType != 3)
        {
            return (null, null);
        }

        ushort domainLength = BitConverter.ToUInt16(data, 28);
        int domainOffset = BitConverter.ToInt32(data, 32);

        ushort userNameLength = BitConverter.ToUInt16(data, 36);
        int userNameOffset = BitConverter.ToInt32(data, 40);

        string? domain = null;
        string? username = null;

        if (domainLength > 0 && domainOffset + domainLength <= data.Length)
        {
            domain = Encoding.Unicode.GetString(data, domainOffset, domainLength);
        }

        if (userNameLength > 0 && userNameOffset + userNameLength <= data.Length)
        {
            username = Encoding.Unicode.GetString(data, userNameOffset, userNameLength);
        }

        return (domain, username);
    }

    public override string GetDomain()
    {
        return ServerDomain;
    }

    public override string GetPackageName()
    {
        return nameof(NTLM);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_ctxtHandleValid)
        {
            Sspi.DeleteSecurityContext(ref _ctxtHandle);
            _ctxtHandleValid = false;
        }

        Sspi.FreeCredentialsHandle(ref _credHandle);

        if (_validationCredHandleValid)
        {
            Sspi.FreeCredentialsHandle(ref _validationCredHandle);
            _validationCredHandleValid = false;
        }

        GC.SuppressFinalize(this);
    }

    ~NTLM()
    {
        Dispose();
    }
}
