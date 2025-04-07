using System.Text;

namespace Irc.Helpers;

public static class ByteExtensions
{
    // Because ASCIIEncoding.ASCII.GetString(bytes); is unreliable and returns ????
    public static string ToAsciiString(this byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes) sb.Append((char)b);
        return sb.ToString();
    }

    public static string ToUnicodeString(this byte[] bytes)
    {
        var unicodeBytes = Encoding.Convert(Encoding.ASCII, Encoding.Unicode, bytes);
        return new string(unicodeBytes.Select(c => (char)c).ToArray());
    }
}