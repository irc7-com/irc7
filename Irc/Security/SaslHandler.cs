using System.Runtime.InteropServices;
using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;
using Irc.Security.Credentials;
using Irc.Security.Passport;
using IrcxSspi.Interop;
using IrcxSspi.Native;
using NLog;

namespace Irc.Security;

// TODO: For GateKeeper / GateKeeperPassport
// You'll need to make sure that GateKeeper is non null GKID, and GateKeeperPassport is null.

// TODO: For NTLMPassport (Guest = false, Subscribed = true)
// e.g. can be any passport login, will have the details of those credentials
// Only diff is will have the privs of the original login via NTLM
// e.g. :Ver|zon!4AB44E8AA2D222EA@GateKeeperPassport JOIN H,A,RXB,. %#The\bLobby

public class SaslHandler : ISaslHandler
{
    private sealed class Session : IDisposable
    {
        public string? ActivePackage;
        public IrcxSspiNative.CtxtHandle Context;
        public bool HasContext;
        public IrcxSspiNative.CredHandle Cred;
        public bool HasCred;

        public void Reset()
        {
            ActivePackage = null;
            HasContext = false;
            Context = default;
            HasCred = false;
            Cred = default;
        }

        public void Dispose()
        {
            if (HasContext)
            {
                var ctx = Context;
                _ = IrcxSspiNative.DeleteSecurityContext(ref ctx);
                HasContext = false;
                Context = default;
            }
            if (HasCred)
            {
                var cred = Cred;
                _ = IrcxSspiNative.FreeCredentialsHandle(ref cred);
                HasCred = false;
                Cred = default;
            }
            ActivePackage = null;
        }
    }
    
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    protected ICredential? Credentials;
    public bool Guest = true;
    public bool Listed = true;
    public EnumSupportPackageSequence ServerSequence;

    public uint ServerVersion;
    private uint _attr;
    private ulong _expiry;
    private IrcxSspiNative.CtxtHandle _serverCtx;
    private IrcxSspiNative.CredHandle _serverCred;
    private byte[] _authResponse;
    private string _package;
    private readonly Dictionary<string, PermissionProfile> _permissionProfiles;
    private Session session = new();
    public bool Authenticated { get; protected set; }
    public bool RequiresPassport { get; set; }
    public bool PendingPassportCreds { get; set; }
    public PassportProvider PassportProvider { get; set; }

    public SaslHandler()
    {
        _permissionProfiles = new Dictionary<string, PermissionProfile>(StringComparer.OrdinalIgnoreCase);
    }
    
    public SaslHandler(Dictionary<string, PermissionProfile>? permissionProfiles, bool passport)
    {
        _permissionProfiles = permissionProfiles != null
            ? new Dictionary<string, PermissionProfile>(permissionProfiles, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, PermissionProfile>(StringComparer.OrdinalIgnoreCase);
        RequiresPassport = passport;
    }
    
    public virtual string GetAuthResponse()
    {
        return _authResponse.ToAsciiString();
    }

    public bool ValidatePassportCredentials(string package, string ticket, string profile)
    {
        // Passport is always supplementary to an auth package, if no Credentials then we should bail here
        if (Credentials == null) return false;
        
        var passportCredentials = PassportProvider.ValidateTokens(new Dictionary<string, string> { { "ticket", ticket }, { "profile", profile } });
        if (passportCredentials == null)
        {
            PendingPassportCreds = true;
            return false;
        }

        var permissionsFound = TryResolvePermissionProfile(passportCredentials.Domain, package, out var permissionProfile);
        if (!permissionsFound) return false;
        if (!IsProtocolAllowed(permissionProfile, package)) return false;

        var oldPermissions = Credentials.PermissionProfile;
        Credentials = passportCredentials;

        if (Credentials.PermissionProfile != null)
        {
            // If the new permissions are higher than the old ones, then we should update the old permissions to the new ones
            if (oldPermissions.Level > Credentials.PermissionProfile.Level) Credentials.PermissionProfile = oldPermissions;
        }

        // Passport users are not a guest
        Guest = false;
        return true;
    }

    public virtual EnumSupportPackageSequence InitializeSecurityContext(string package, string token, string ip)
    {
        _package = package;
        session.Dispose();
        session.Reset();
        session.ActivePackage = package;

        var rcCred = SspiHelpers.AcquireInboundCredentials(package, out var cred);
        if (rcCred != 0)
            throw new InvalidOperationException($"AcquireCredentialsHandleA failed: 0x{rcCred:X8}");
        session.Cred = cred;
        session.HasCred = true;

        IrcxSspiNative.CtxtHandle? initialContext = null;
        var (rc, newCtx, outToken) = Accept(session.Cred, context: ref initialContext, token, ip, includePkgParams: true);
        if (rc == (int)IrcxSspiNative.SspiError.ContinueNeeded)
        {
            session.Context = newCtx;
            session.HasContext = true;
            _authResponse = outToken;
            return EnumSupportPackageSequence.SSP_EXT;
        }
        if (rc == 0)
        {
            var ctx = newCtx;
            var credentials = SspiHelpers.QueryContextUserName(ref ctx) ?? "Unknown";
            _ = IrcxSspiNative.DeleteSecurityContext(ref ctx);
            session.Dispose();
            session.Reset();

            return EnumSupportPackageSequence.SSP_OK;
        }

        Log.Debug($"AcceptSecurityContext failed: 0x{rc:X8}");
        return EnumSupportPackageSequence.SSP_FAILED;
    }
    
      private static (int Rc, IrcxSspiNative.CtxtHandle NewContext, byte[] OutToken) Accept(
        IrcxSspiNative.CredHandle cred,
        ref IrcxSspiNative.CtxtHandle? context,
        string token,
        string ip,
        bool includePkgParams)
      {
        var tokenBytes = token.ToByteArray();
        IrcxSspiNative.CtxtHandle newCtx = default;
        uint attrs = 0;
        nuint expiry = 0;
        var rc = 0;
        var actualLen = 0;

        var outToken = new byte[4096];
        var inputBuffersCount = includePkgParams ? 3 : 1;
        var secBufferSize = Marshal.SizeOf<IrcxSspiNative.SecBuffer>();

        IntPtr pToken = IntPtr.Zero;
        IntPtr pHost = IntPtr.Zero;
        IntPtr pCompat = IntPtr.Zero;
        IntPtr pInBuffers = IntPtr.Zero;
        IntPtr pInDesc = IntPtr.Zero;
        IntPtr pOutToken = IntPtr.Zero;
        IntPtr pOutBuffers = IntPtr.Zero;
        IntPtr pOutDesc = IntPtr.Zero;
        IntPtr pContext = IntPtr.Zero;

        try
        {
          pToken = Marshal.AllocHGlobal(tokenBytes.Length);
          Marshal.Copy(tokenBytes, 0, pToken, tokenBytes.Length);

          pOutToken = Marshal.AllocHGlobal(outToken.Length);

          pInBuffers = Marshal.AllocHGlobal(secBufferSize * inputBuffersCount);
          Marshal.StructureToPtr(new IrcxSspiNative.SecBuffer
          {
            BufferType = IrcxSspiNative.SECBUFFER_TOKEN,
            cbBuffer = (uint)tokenBytes.Length,
            pvBuffer = pToken
          }, pInBuffers, false);

          if (includePkgParams)
          {
            pHost = Marshal.StringToHGlobalAnsi(ip);
            pCompat = Marshal.AllocHGlobal(1);
            Marshal.WriteByte(pCompat, 1);

            Marshal.StructureToPtr(new IrcxSspiNative.SecBuffer
            {
              BufferType = IrcxSspiNative.SECBUFFER_PKG_PARAMS,
              cbBuffer = (uint)ip.Length,
              pvBuffer = pHost
            }, IntPtr.Add(pInBuffers, secBufferSize), false);

            Marshal.StructureToPtr(new IrcxSspiNative.SecBuffer
            {
              BufferType = IrcxSspiNative.SECBUFFER_PKG_PARAMS,
              cbBuffer = 1,
              pvBuffer = pCompat
            }, IntPtr.Add(pInBuffers, secBufferSize * 2), false);
          }

          var inDesc = new IrcxSspiNative.SecBufferDesc
          {
            ulVersion = IrcxSspiNative.SECBUFFER_VERSION,
            cBuffers = (uint)inputBuffersCount,
            pBuffers = pInBuffers
          };
          pInDesc = Marshal.AllocHGlobal(Marshal.SizeOf<IrcxSspiNative.SecBufferDesc>());
          Marshal.StructureToPtr(inDesc, pInDesc, false);

          pOutBuffers = Marshal.AllocHGlobal(secBufferSize);
          Marshal.StructureToPtr(new IrcxSspiNative.SecBuffer
          {
            BufferType = IrcxSspiNative.SECBUFFER_TOKEN,
            cbBuffer = (uint)outToken.Length,
            pvBuffer = pOutToken
          }, pOutBuffers, false);

          var outDesc = new IrcxSspiNative.SecBufferDesc
          {
            ulVersion = IrcxSspiNative.SECBUFFER_VERSION,
            cBuffers = 1,
            pBuffers = pOutBuffers
          };
          pOutDesc = Marshal.AllocHGlobal(Marshal.SizeOf<IrcxSspiNative.SecBufferDesc>());
          Marshal.StructureToPtr(outDesc, pOutDesc, false);

          if (context.HasValue)
          {
            pContext = Marshal.AllocHGlobal(Marshal.SizeOf<IrcxSspiNative.CtxtHandle>());
            Marshal.StructureToPtr(context.Value, pContext, false);
          }

          rc = IrcxSspiNative.AcceptSecurityContext(
            ref cred,
            pContext,
            pInDesc,
            0,
            IrcxSspiNative.SECURITY_NATIVE_DREP,
            ref newCtx,
            pOutDesc,
            ref attrs,
            ref expiry);

          var outBuffer = Marshal.PtrToStructure<IrcxSspiNative.SecBuffer>(pOutBuffers);
          actualLen = (int)Math.Min((uint)outToken.Length, outBuffer.cbBuffer);
          if (actualLen > 0)
            Marshal.Copy(outBuffer.pvBuffer, outToken, 0, actualLen);
        }
        finally
        {
          if (pContext != IntPtr.Zero) Marshal.FreeHGlobal(pContext);
          if (pOutDesc != IntPtr.Zero) Marshal.FreeHGlobal(pOutDesc);
          if (pOutBuffers != IntPtr.Zero) Marshal.FreeHGlobal(pOutBuffers);
          if (pOutToken != IntPtr.Zero) Marshal.FreeHGlobal(pOutToken);
          if (pInDesc != IntPtr.Zero) Marshal.FreeHGlobal(pInDesc);
          if (pInBuffers != IntPtr.Zero) Marshal.FreeHGlobal(pInBuffers);
          if (pCompat != IntPtr.Zero) Marshal.FreeHGlobal(pCompat);
          if (pHost != IntPtr.Zero) Marshal.FreeHGlobal(pHost);
          if (pToken != IntPtr.Zero) Marshal.FreeHGlobal(pToken);
        }

        if (actualLen < 0) actualLen = 0;
        Array.Resize(ref outToken, actualLen);
        return (rc, newCtx, outToken);
      }

    public virtual EnumSupportPackageSequence AcceptSecurityContext(string package, string token, string ip)
    {
        if (!session.HasContext || session.ActivePackage is null || !string.Equals(session.ActivePackage, package, StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid session state");

        if (!session.HasCred)
            throw new InvalidOperationException("Missing credentials handle");

        IrcxSspiNative.CtxtHandle? ctx = session.Context;
        var (rc, newCtx, outToken) = Accept(session.Cred, context: ref ctx, token, ip, includePkgParams: false);
        if (rc == (int)IrcxSspiNative.SspiError.ContinueNeeded)
        {
            session.Context = newCtx;
            session.HasContext = true;
            return EnumSupportPackageSequence.SSP_EXT;
        }
        if (rc == 0)
        {
            var finalCtx = newCtx;
            var identity = SspiHelpers.QueryContextUserName(ref finalCtx) ?? "Unknown";
            _ = IrcxSspiNative.DeleteSecurityContext(ref finalCtx);
            session.Dispose();
            session.Reset();

            var (domain, username) = ParseIdentity(identity, package);

            if (!TryResolvePermissionProfile(identity, package, out var permissionProfile))
            {
                Log.Debug($"No permission profile found for identity '{identity}' or package '{package}'.");
                return EnumSupportPackageSequence.SSP_FAILED;
            }

            if (!IsProtocolAllowed(permissionProfile, package))
            {
                Log.Debug($"Permission profile for '{identity}' does not allow protocol '{package}'.");
                return EnumSupportPackageSequence.SSP_FAILED;
            }

            Credentials = BuildCredential(username, domain, permissionProfile);
            
            // MSN CAC will PROP its nickname after NTLM auth, the below bit is to ensure there is a nick
            if (package == "NTLM")
            {
                Credentials.Nickname = username;
            }
            
            Guest = permissionProfile.Guest;
            Authenticated = true;
            return EnumSupportPackageSequence.SSP_OK;
        }

        // On failure, clean up the old context.
        {
            var old = session.Context;
            _ = IrcxSspiNative.DeleteSecurityContext(ref old);
            session.Dispose();
            session.Reset();
        }
        Log.Debug($"AcceptSecurityContext failed: 0x{rc:X8}");

        return EnumSupportPackageSequence.SSP_FAILED;
    }

    public virtual string GetDomain()
    {
        return GetPackageName();
    }

    public virtual string GetPackageName()
    {
        return _package;
    }

    public virtual ICredential? GetCredentials()
    {
        return Credentials;
    }

    public void SetCredentials(ICredential? credentials)
    {
        Credentials = credentials;
    }

    public bool IsAuthenticated()
    {
        return Authenticated;
    }



    public void Reset()
    {
        session.Dispose();
        session.Reset();
        Authenticated = false;
        Credentials = null;
        RequiresPassport = false;
        PendingPassportCreds = false;
    }

    private bool TryResolvePermissionProfile(string identity, string package, out PermissionProfile permissionProfile)
    {
        if (_permissionProfiles.TryGetValue(identity, out permissionProfile!))
            return true;

        return _permissionProfiles.TryGetValue(package, out permissionProfile!);
    }

    private static bool IsProtocolAllowed(PermissionProfile permissionProfile, string package)
    {
        if (permissionProfile.Protocols == null || permissionProfile.Protocols.Count == 0)
            return false;

        foreach (var protocol in permissionProfile.Protocols)
        {
            if (string.Equals(protocol, package, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static (string Domain, string Username) ParseIdentity(string identity, string fallbackDomain)
    {
        var separator = identity.IndexOf('\\');
        if (separator > 0 && separator < identity.Length - 1)
            return (identity[..separator], identity[(separator + 1)..]);

        return (fallbackDomain, identity);
    }

    private static Credential BuildCredential(string username, string domain, PermissionProfile permissionProfile)
    {
        return new Credential
        {
            Username = username,
            Domain = domain,
            Modes = permissionProfile.Modes,
            Prefix = permissionProfile.Prefix,
            Level = permissionProfile.Level,
            Guest = permissionProfile.Guest,
            PermissionProfile = permissionProfile
        };
    }
}