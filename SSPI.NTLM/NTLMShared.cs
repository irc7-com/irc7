using System.Runtime.InteropServices;
using Irc.Helpers;

namespace SSPI.NTLM;

public class NtlmShared
{
    public static readonly string NtlmSignature = "NTLMSSP\0";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NtlmsspSecurityBuffer
    {
        [MarshalAs(UnmanagedType.I2)] public short Length;
        [MarshalAs(UnmanagedType.I2)] public short AllocatedSpace;
        [MarshalAs(UnmanagedType.I4)] public int Offset;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class NtlmsspSubBlock(short type, short length)
    {
        [MarshalAs(UnmanagedType.I2)] public readonly short Length = length;
        [MarshalAs(UnmanagedType.I2)] public short Type = type;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NtlmssposVersion
    {
        [MarshalAs(UnmanagedType.I1)] public byte Major;
        [MarshalAs(UnmanagedType.I1)] public byte Minor;
        [MarshalAs(UnmanagedType.I2)] public short BuildNumber;
        [MarshalAs(UnmanagedType.I4)] public int Reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NtlMv2BlobStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] BlobSignature;

        [MarshalAs(UnmanagedType.I4)] public int Reserved;
        [MarshalAs(UnmanagedType.I8)] public long Timestamp;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] ClientNonce;

        [MarshalAs(UnmanagedType.I4)] public int Unknown;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 4)]
        public NtlmsspSubBlock TargetInformation;
    }

    public class TargetInformation
    {
        public string DomainName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string DnsDomainName { get; set; } = string.Empty;
        public string DnsServerName { get; set; } = string.Empty;
    }

    public class NtlMv2Blob
    {
        public NtlMv2Blob(string ntlmBlobData)
        {
            Digest(ntlmBlobData);
        }

        public NtlMv2BlobStruct DeserializedBlob { get; private set; }
        public string ClientHashResult { get; private set; } = string.Empty;
        public byte[] ClientSignature { get; private set; } = Array.Empty<byte>();
        public byte[] ClientNonce { get; private set; } = Array.Empty<byte>();
        public long ClientTimestamp { get; private set; }
        public string ClientTarget { get; private set; } = string.Empty;
        public string BlobData { get; private set; } = string.Empty;

        private void Digest(string blobData)
        {
            if (blobData.Length >= 16)
            {
                ClientHashResult = blobData.Substring(0, 16);
                BlobData = blobData.Substring(16);

                var blobHeaderSize = Marshal.SizeOf<NtlMv2BlobStruct>();
                if (BlobData.Length >= blobHeaderSize)
                {
                    var blobHeaderData = BlobData.Substring(0, blobHeaderSize);
                    var blobPayload = BlobData.Substring(blobHeaderSize);

                    DeserializedBlob = blobHeaderData.ToByteArray().Deserialize<NtlMv2BlobStruct>();

                    ClientSignature = DeserializedBlob.BlobSignature;
                    ClientNonce = DeserializedBlob.ClientNonce;
                    ClientTimestamp = DeserializedBlob.Timestamp;

                    if (blobPayload.Length >= DeserializedBlob.TargetInformation.Length)
                        ClientTarget =
                            blobPayload.Substring(0, DeserializedBlob.TargetInformation.Length);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NTLMSSPMessageType1
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Signature;

        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint Type;

        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint Flags;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NtlmsspSecurityBuffer SuppliedDomain;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NtlmsspSecurityBuffer SuppliedWorkstation;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NtlmssposVersion OSVersionInfo;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NTLMSSPMessageType2
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Signature;

        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint Type;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NtlmsspSecurityBuffer TargetName;

        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Challenge;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Context;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NtlmsspSecurityBuffer TargetInformation;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NTLMSSPMessageType3
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Signature;

        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint Type;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NtlmsspSecurityBuffer LMResponse;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NtlmsspSecurityBuffer NTLMResponse;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NtlmsspSecurityBuffer TargetName;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NtlmsspSecurityBuffer UserName;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NtlmsspSecurityBuffer WorkstationName;
    }
}