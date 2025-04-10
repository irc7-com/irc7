using System;
using Irc.Helpers;
using NUnit.Framework;

namespace SSPI.NTLM.Tests;

public class NTLMV2ResponseTests
{
    //[Test, Ignore("Temporarily disabled")]
    public void NTLMv2Response_Test()
    {
        var expectedResult = new byte[]
            { 0xcb, 0xab, 0xbc, 0xa7, 0x13, 0xeb, 0x79, 0x5d, 0x04, 0xc9, 0x7a, 0xbc, 0x01, 0xee, 0x49, 0x83 };

        var username = "user".ToUnicodeString();
        var password = "SecREt01".ToUnicodeString();
        var challenge = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef };
        var blob = new byte[] //Hash from client passed in the NTLM hash field of Message3
        {
            0xcb, 0xab, 0xbc, 0xa7, 0x13, 0xeb, 0x79, 0x5d, 0x04, 0xc9, 0x7a, 0xbc, 0x01, 0xee, 0x49, 0x83,
            0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x90, 0xd3, 0x36, 0xb7, 0x34, 0xc3, 0x01,
            0xff, 0xff, 0xff, 0x00, 0x11, 0x22, 0x33, 0x44, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x0c, 0x00,
            0x44, 0x00, 0x4f, 0x00, 0x4d, 0x00, 0x41, 0x00, 0x49, 0x00, 0x4e, 0x00, 0x01, 0x00, 0x0c, 0x00,
            0x53, 0x00, 0x45, 0x00, 0x52, 0x00, 0x56, 0x00, 0x45, 0x00, 0x52, 0x00, 0x04, 0x00, 0x14, 0x00,
            0x64, 0x00, 0x6f, 0x00, 0x6d, 0x00, 0x61, 0x00, 0x69, 0x00, 0x6e, 0x00, 0x2e, 0x00, 0x63, 0x00,
            0x6f, 0x00, 0x6d, 0x00, 0x03, 0x00, 0x22, 0x00, 0x73, 0x00, 0x65, 0x00, 0x72, 0x00, 0x76, 0x00,
            0x65, 0x00, 0x72, 0x00, 0x2e, 0x00, 0x64, 0x00, 0x6f, 0x00, 0x6d, 0x00, 0x61, 0x00, 0x69, 0x00,
            0x6e, 0x00, 0x2e, 0x00, 0x63, 0x00, 0x6f, 0x00, 0x6d, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00
        };

        var ntlmAlgorithms = new NtlmResponses();
        var result = ntlmAlgorithms.NtlmV2Response(username, password, challenge.ToAsciiString(), blob.ToAsciiString());

        Assert.That(expectedResult.ToAsciiString(), Is.EqualTo(result));
    }

    //[Test, Ignore("Temporarily disabled")]
    public void NTLMv2Response_CACTest1()
    {
        var expectedResult = "ö\\4ùæ\u009f¿\u0082*\u0004-l¼8¢\u009d";

        var dataBlob =
            "ö\\4ùæ\u009f¿\u0082*\u0004-l¼8¢\u009d\u0001\u0001\0\0\0\0\0\0/Çx\u009e\u0003\rØ\u0001dª(\rÌ\u008dü.\0\0\0\0\u0002\0\f\0D\0O\0M\0A\0I\0N\0\0\0\0\0\0\0\0\0";

        var serverChallenge = "AAAAAAAA";
        var username = "username";
        var password = "password";

        var ntlmAlgorithms = new NtlmResponses();
        var result = ntlmAlgorithms.NtlmV2Response(username.ToUnicodeString(), password.ToUnicodeString(),
            serverChallenge, dataBlob);

        Assert.That(expectedResult, Is.EqualTo(result));
    }

    //[Test, Ignore("Temporarily disabled")]
    public void NTLMv2Response_CACTest2()
    {
        var expectedResult = "æ½®7ª®dxµ1Ô!o?ÉÓ";

        var dataBlob =
            "æ½®7ª®dxµ1Ô!o?ÉÓ\u0001\u0001\0\0\0\0\0\0~{×:ú\fØ\u0001~id°s³\u0082\u001c\0\0\0\0\u0002\0\f\0D\0O\0M\0A\0I\0N\0\0\0\0\0\0\0\0\0";

        var serverChallenge = "AAAAAAAA";
        var username = "username";
        var password = "password";

        var ntlmAlgorithms = new NtlmResponses();
        var result = ntlmAlgorithms.NtlmV2Response(username.ToUnicodeString(), password.ToUnicodeString(),
            serverChallenge, dataBlob);

        Assert.That(expectedResult, Is.EqualTo(result));
    }

    [Test]
    public void NTLMv2MSResponse_BlankUsernameThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new NtlmResponses().NtlmV2Response(string.Empty, "A", "A", "A"));
    }

    [Test]
    public void NTLMv2MSResponse_BlankPasswordThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new NtlmResponses().NtlmV2Response("A", string.Empty, "A", "A"));
    }

    [Test]
    public void NTLMv2MSResponse_BlankChallengeThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new NtlmResponses().NtlmV2Response("A", "A", string.Empty, "A"));
    }

    [Test]
    public void NTLMv2MSResponse_BlankBlobThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new NtlmResponses().NtlmV2Response("A", "A", "A", string.Empty));
    }

    [Test]
    public void NTLMv2MSResponse_HashOnlyDoesNotThrowException()
    {
        Assert.DoesNotThrow(() => new NtlmResponses().NtlmV2Response("A", "A", "A", new string('A', 16)));
    }

    [Test]
    public void NTLMv2MSResponse_GarbledStringDoesNotThrowException()
    {
        Assert.DoesNotThrow(() => new NtlmResponses().NtlmV2Response("A", "A", "A", new string('A', 1024)));
    }
}