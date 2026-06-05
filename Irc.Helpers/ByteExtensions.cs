using System.Text;

namespace Irc.Helpers;

public static class ByteExtensions
{
    public static string ToAsciiString(this byte[] bytes)
    {
        return Encoding.Latin1.GetString(bytes);
    }

    public static string ToUnicodeString(this byte[] bytes)
    {
        var unicodeBytes = Encoding.Convert(Encoding.ASCII, Encoding.Unicode, bytes);
        return new string(unicodeBytes.Select(c => (char)c).ToArray());
    }
}