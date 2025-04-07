using System;
using Irc.Helpers;
using NUnit.Framework;

namespace SSPI.NTLM.Tests;

public class NTLMResponseTests
{
    [Test]
    public void NTLMResponse_Test()
    {
        var expectedResult = new byte[]
        {
            0x25, 0xa9, 0x8c, 0x1c, 0x31, 0xe8, 0x18, 0x47, 0x46, 0x6b, 0x29, 0xb2, 0xdf, 0x46, 0x80, 0xf3, 0x99, 0x58,
            0xfb, 0x8c, 0x21, 0x3a, 0x9c, 0xc6
        };

        var password = "SecREt01".ToUnicodeString();
        var challenge = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };

        var ntlmAlgorithms = new NtlmResponses();
        var result = ntlmAlgorithms.NtlmResponse(password, challenge.ToAsciiString());

        Assert.That(expectedResult, Is.EqualTo(result));
    }

    [Test]
    public void NTLMResponse_BlankPasswordThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new NtlmResponses().NtlmResponse(string.Empty, "challenge"));
    }

    [Test]
    public void NTLMResponse_BlankChallengeThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new NtlmResponses().NtlmResponse("password", string.Empty));
    }
}