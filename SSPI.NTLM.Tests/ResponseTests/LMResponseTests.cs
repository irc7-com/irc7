using System;
using System.Security.Cryptography;
using Irc.Helpers;
using NUnit.Framework;

namespace SSPI.NTLM.Tests;

public class LMResponseTests
{
    [Test]
    public void LMResponse_Test()
    {
        var expectedResult = new byte[]
        {
            0xc3, 0x37, 0xcd, 0x5c, 0xbd, 0x44, 0xfc, 0x97, 0x82, 0xa6, 0x67, 0xaf, 0x6d, 0x42, 0x7c, 0x6d, 0xe6, 0x7c,
            0x20, 0xc2, 0xd3, 0xe7, 0x7c, 0x56
        };

        var password = "SecREt01";
        var challenge = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };

        var ntlmAlgorithms = new NtlmResponses();
        var result = ntlmAlgorithms.LmResponse(password, challenge.ToAsciiString());

        Assert.That(expectedResult.ToAsciiString(), Is.EqualTo(result));
    }

    [Test]
    public void LMResponse_PasswordExceededLengthThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new NtlmResponses().LmResponse(new string('A', NtlmResponses.LmMaxPasswordLength + 1), "challenge"));
    }

    [Test]
    public void LMResponse_WeakPasswordThrowsException()
    {
        Assert.Throws<CryptographicException>(() =>
            new NtlmResponses().LmResponse(new string('\0', NtlmResponses.LmMaxPasswordLength), "challenge"));
    }

    [Test]
    public void LMResponse_BlankPasswordThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new NtlmResponses().LmResponse(string.Empty, "challenge"));
    }

    [Test]
    public void LMResponse_BlankChallengeThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new NtlmResponses().LmResponse("password", string.Empty));
    }
}