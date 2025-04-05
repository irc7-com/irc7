using System;
using System.Runtime.InteropServices;
using Irc.Helpers;
using NUnit.Framework;

namespace Irc.Extensions.NTLM.Tests.MessageTests;

internal class NtlmType1MessageTests
{
    [Test]
    public void NtlmType1Message_BlankStringThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new NtlmType1Message(string.Empty));
    }

    [Test]
    public void NtlmType1Message_InsufficientMessageLengthThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new NtlmType1Message(new string('A', Marshal.SizeOf<NtlmShared.NTLMSSPMessageType1>() - 1)));
    }

    [Test]
    public void NtlmType1Message_DoesNotStartWithSignatureThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new NtlmType1Message(new string('A', Marshal.SizeOf<NtlmShared.NTLMSSPMessageType1>())));
    }

    [Test]
    public void NtlmType1Message_CACDeserializesOK()
    {
        var ntlmMessage1 =
            new NtlmType1Message("NTLMSSP\0\u0001\0\0\0\a\u0082\b¢\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\n\0ðU\0\0\0\u000f");

        Assert.That(ntlmMessage1.Signature.ToAsciiString(), Is.EqualTo(NtlmShared.NtlmSignature));
        Assert.That(ntlmMessage1.ClientVersion.Major, Is.EqualTo(10));
        Assert.That(ntlmMessage1.EnumeratedFlags[NtlmFlag.NTLMSSP_NEGOTIATE_NTLM2], Is.True);
    }

    [Test]
    public void NtlmType1Message_CACDeserializeOKOn95IE3()
    {
        var ntlmMessage1 =
            new NtlmType1Message(
                "NTLMSSP\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\00\0\0\0\0\08\0\0\0username172.22.1.18:6667");
    }
}
