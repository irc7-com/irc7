using System.Text;

namespace Irc.Helpers;

public static class StringExtensions
{
    public static byte[] ToByteArray(this string text, int offset, int count)
    {
        var b = new byte[count];
        for (var i = 0; i < count; i++) b[i] = (byte)text[i];
        return b;
    }

    public static byte[] ToByteArray(this string text)
    {
        return text.ToByteArray(0, text.Length);
    }

    public static byte[] GetBytes(this StringBuilder text)
    {
        var b = new byte[text.Length];
        for (var i = 0; i < text.Length; i++) b[i] = (byte)text[i];

        return b;
    }

    public static string ToUnicodeString(this string text)
    {
        return ToByteArray(text).ToUnicodeString();
    }

    public static string toutf16(byte[] data)
    {
        return Encoding.Unicode.GetString(data);
    }

    public static string ToLiteral(this string data)
    {
        var literal = new StringBuilder(data.Length);

        for (var i = 0; i < data.Length; i++)
            switch (data[i])
            {
                case '\\':
                {
                    i++;
                    if (i < data.Length)
                        switch (data[i])
                        {
                            case 'b':
                            {
                                literal.Append(' ');
                                break;
                            }
                            case '0':
                            {
                                literal.Append('\0');
                                ;
                                break;
                            }
                            case 't':
                            {
                                literal.Append('\t');
                                break;
                            }
                            case 'c':
                            {
                                literal.Append(',');
                                break;
                            }
                            case '\\':
                            {
                                literal.Append('\\');
                                break;
                            }
                            case 'r':
                            {
                                literal.Append('\r');
                                break;
                            }
                            case 'n':
                            {
                                literal.Append('\n');
                                break;
                            }
                            default:
                            {
                                literal.Append(data[i - 1]);
                                literal.Append(data[i]);
                                break;
                            }
                        }
                    else
                        literal.Append(data[i - 1]);

                    break;
                }
                default:
                {
                    literal.Append(data[i]);
                    break;
                }
            }

        return literal.ToString();
    }

    public static string ToEscape(this string data)
    {
        var escape = new StringBuilder(data.Length * 2);

        for (var i = 0; i < data.Length; i++)
            switch (data[i])
            {
                case (char)0x0:
                    {
                        escape.Append('\\');
                        escape.Append('0');
                        break;
                    }
                case (char)0x9:
                    {
                        escape.Append('\\');
                        escape.Append('t');
                        break;
                    }
                case (char)0xD:
                    {
                        escape.Append('\\');
                        escape.Append('r');
                        break;
                    }
                case (char)0xA:
                    {
                        escape.Append('\\');
                        escape.Append('n');
                        break;
                    }
                case (char)0x20:
                    {
                        escape.Append('\\');
                        escape.Append('b');
                        break;
                    }
                case (char)0x2C:
                    {
                        escape.Append('\\');
                        escape.Append('c');
                        break;
                    }
                case (char)0x5C:
                    {
                        escape.Append('\\');
                        escape.Append('\\');
                        break;
                    }
                default:
                    {
                        escape.Append(data[i]);
                        break;
                    }
            }

        return escape.ToString();
    }

    public static StringBuilder FromBytes(byte[] bytes)
    {
        var stringBuilder = new StringBuilder(bytes.Length);
        for (var i = 0; i < bytes.Length; i++) stringBuilder.Append((char)bytes[i]);
        return stringBuilder;
    }

    public static StringBuilder FromBytes(byte[] bytes, int start, int count)
    {
        var stringBuilder = new StringBuilder(count);
        for (var i = start; i < count; i++) stringBuilder.Append((char)bytes[i]);
        return stringBuilder;
    }

    public static char[] BytesToChars(byte[] bytes, int offset, int count)
    {
        var c = new char[count];
        for (var i = 0; i < count; i++) c[i] = (char)bytes[offset + i];
        return c;
    }

    public static char[] BytesToChars(byte[] bytes)
    {
        return BytesToChars(bytes, 0, bytes.Length);
    }
}