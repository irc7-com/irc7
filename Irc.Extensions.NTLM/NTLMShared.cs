using System.Runtime.InteropServices;
using Irc.Helpers;

namespace Irc.Extensions.NTLM;

public class NTLMShared
{
    public static string NTLMSignature = "NTLMSSP\0";

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NTLMSSPSecurityBuffer
    {
        [MarshalAs(UnmanagedType.I2)] public short Length;
        [MarshalAs(UnmanagedType.I2)] public short AllocatedSpace;
        [MarshalAs(UnmanagedType.I4)] public int Offset;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class NTLMSSPSubBlock
    {
        [MarshalAs(UnmanagedType.I2)] public short Length;
        [MarshalAs(UnmanagedType.I2)] public short Type;

        public NTLMSSPSubBlock(short type, short length)
        {
            Type = type;
            Length = length;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NTLMSSPOSVersion
    {
        [MarshalAs(UnmanagedType.I1)] public byte Major;
        [MarshalAs(UnmanagedType.I1)] public byte Minor;
        [MarshalAs(UnmanagedType.I2)] public short BuildNumber;
        [MarshalAs(UnmanagedType.I4)] public int Reserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NTLMv2BlobStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] BlobSignature;

        [MarshalAs(UnmanagedType.I4)] public int Reserved;
        [MarshalAs(UnmanagedType.I8)] public long Timestamp;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] ClientNonce;

        [MarshalAs(UnmanagedType.I4)] public int Unknown;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 4)]
        public NTLMSSPSubBlock TargetInformation;
    }

    public class TargetInformation
    {
        public string DomainName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string DnsDomainName { get; set; } = string.Empty;
        public string DnsServerName { get; set; } = string.Empty;
    }

    public class NTLMv2Blob
    {
        public NTLMv2Blob(string ntlmBlobData)
        {
            Digest(ntlmBlobData);
        }

        public NTLMv2BlobStruct DeserializedBlob { get; private set; }
        public string ClientHashResult { get; private set; }
        public byte[] ClientSignature { get; private set; }
        public byte[] ClientNonce { get; private set; }
        public long ClientTimestamp { get; private set; }
        public string ClientTarget { get; private set; }
        public string BlobData { get; private set; }

        private void Digest(string blobData)
        {
            if (blobData.Length >= 16)
            {
                ClientHashResult = blobData.Substring(0, 16);
                BlobData = blobData.Substring(16);

                var blobHeaderSize = Marshal.SizeOf<NTLMv2BlobStruct>();
                if (BlobData.Length >= blobHeaderSize)
                {
                    var blobHeaderData = BlobData.Substring(0, blobHeaderSize);
                    var blobPayload = BlobData.Substring(blobHeaderSize);

                    DeserializedBlob = blobHeaderData.ToByteArray().Deserialize<NTLMv2BlobStruct>();

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
        public NTLMSSPSecurityBuffer SuppliedDomain;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NTLMSSPSecurityBuffer SuppliedWorkstation;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NTLMSSPOSVersion OSVersionInfo;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NTLMSSPMessageType2
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Signature;

        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint Type;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NTLMSSPSecurityBuffer TargetName;

        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Challenge;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Context;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NTLMSSPSecurityBuffer TargetInformation;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct NTLMSSPMessageType3
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Signature;

        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        public uint Type;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NTLMSSPSecurityBuffer LMResponse;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NTLMSSPSecurityBuffer NTLMResponse;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NTLMSSPSecurityBuffer TargetName;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NTLMSSPSecurityBuffer UserName;

        [MarshalAs(UnmanagedType.Struct, SizeConst = 8)]
        public NTLMSSPSecurityBuffer WorkstationName;
    }
}