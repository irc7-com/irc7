using System.Runtime.InteropServices;

public struct GateKeeperToken
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
    public byte[] Signature;

    public ushort TimeStamp;
    public uint Version;
    public uint Sequence;

    // Consider adding below for Type 2 message
    //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    //public byte[] Challenge;
}