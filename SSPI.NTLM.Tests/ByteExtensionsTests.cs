using System.Text;
using Irc.Helpers;
using NUnit.Framework;

namespace SSPI.NTLM.Tests;

public class ByteExtensionsTests
{
    [Test]
    public void ToAsciiString_UsesLatin1Decoding()
    {
        var bytes = new byte[] { 0x41, 0xE9, 0xFF };

        var result = bytes.ToAsciiString();

        Assert.That(result, Is.EqualTo(Encoding.Latin1.GetString(bytes)));
        Assert.That(result, Is.EqualTo("Aéÿ"));
    }
}
