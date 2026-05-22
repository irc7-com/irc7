using System.Runtime.InteropServices;

namespace Irc.Security.Sspi;

public static class IrcxSspiNative
{
	public enum SspiError : int
	{
		Ok = 0,
		ContinueNeeded = 0x00090312,
		UnknownCredentials = unchecked((int)0x8009030D),
		InvalidHandle = unchecked((int)0x80090301),
		NotSupported = unchecked((int)0x80090302),
		InvalidToken = unchecked((int)0x80090308),
		LogonDenied = unchecked((int)0x8009030C),
	}

	public const int SECPKG_ATTR_NAMES = 1;

	public const uint SECBUFFER_VERSION = 0;
	public const uint SECBUFFER_TOKEN = 2;
	public const uint SECBUFFER_PKG_PARAMS = 3;

	public const uint SECPKG_CRED_INBOUND = 2;
	public const uint SECURITY_NATIVE_DREP = 16;

	// On Windows, sspi.h defines these as ULONG_PTR fields, i.e. pointer-sized.
	[StructLayout(LayoutKind.Sequential)]
	public struct CredHandle
	{
		public nuint dwLower;
		public nuint dwUpper;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct CtxtHandle
	{
		public nuint dwLower;
		public nuint dwUpper;
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

	[StructLayout(LayoutKind.Sequential)]
	public struct SecPkgContext_NamesA
	{
		public IntPtr sUserName;
	}

	[DllImport("ircx_sspi", CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
	public static extern int AcquireCredentialsHandleA(
		IntPtr pszPrincipal,
		IntPtr pszPackage,
		uint fCredentialUse,
		IntPtr pvLogonId,
		IntPtr pAuthData,
		IntPtr pGetKeyFn,
		IntPtr pvGetKeyArgument,
		ref CredHandle phCredential,
		ref nuint ptsExpiry);

	[DllImport("ircx_sspi", CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
	public static extern int FreeCredentialsHandle(ref CredHandle phCredential);

	[DllImport("ircx_sspi", CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
	public static extern int AcceptSecurityContext(
		ref CredHandle phCredential,
		IntPtr phContext,
		IntPtr pInput,
		uint fContextReq,
		uint TargetDataRep,
		ref CtxtHandle phNewContext,
		IntPtr pOutput,
		ref uint pfContextAttr,
		ref nuint ptsExpiry);

	[DllImport("ircx_sspi", CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
	public static extern int DeleteSecurityContext(ref CtxtHandle phContext);

	[DllImport("ircx_sspi", CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
	public static extern int QueryContextAttributesA(
		ref CtxtHandle phContext,
		uint ulAttribute,
		IntPtr pBuffer);

	[DllImport("ircx_sspi", CallingConvention = CallingConvention.Winapi, ExactSpelling = true)]
	public static extern int FreeContextBuffer(IntPtr pvContextBuffer);
}
