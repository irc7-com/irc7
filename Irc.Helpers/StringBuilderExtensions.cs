using System.Text;

namespace Irc.Helpers;

public static class StringBuilderExtensions
{
    // Object extensions
    public static byte[] ToByteArray(this StringBuilder stringBuilder, int offset, int count)
    {
        var b = new byte[count];
        for (var i = 0; i < count; i++) b[i] = (byte)stringBuilder[i];
        return b;
    }

    public static byte[] ToByteArray(this StringBuilder stringBuilder)
    {
        return stringBuilder.ToByteArray(0, stringBuilder.Length);
    }

    public static int AppendByteAsChar(this StringBuilder stringBuilder, byte b)
    {
        var c = (char)b;
        stringBuilder.Append(c);
        return 1;
    }

    public static int AppendByteArrayAsChars(this StringBuilder stringBuilder, byte[] b, int offset, int count)
    {
        stringBuilder.Append(BytesToChars(b, offset, count));
        return count;
    }

    public static int AppendByteArrayAsChars(this StringBuilder stringBuilder, byte[] b)
    {
        return stringBuilder.AppendByteArrayAsChars(b, 0, b.Length);
    }

    public static StringBuilder ToLiteral(string data)
    {
        var literal = new StringBuilder(data.Length);

        for (var i = 0; i < data.Length; i++)
            switch (data.ToByteArray()[i])
            {
                case (byte)'\\':
                {
                    i++;
                    if (i < data.Length)
                        switch (data.ToByteArray()[i])
                        {
                            case (byte)'b':
                            {
                                literal.Append(' ');
                                break;
                            }
                            case (byte)'0':
                            {
                                literal.Append('\0');
                                ;
                                break;
                            }
                            case (byte)'t':
                            {
                                literal.Append('\t');
                                break;
                            }
                            case (byte)'c':
                            {
                                literal.Append(',');
                                break;
                            }
                            case (byte)'\\':
                            {
                                literal.Append('\\');
                                break;
                            }
                            case (byte)'r':
                            {
                                literal.Append('\r');
                                break;
                            }
                            case (byte)'n':
                            {
                                literal.Append('\n');
                                break;
                            }
                            default:
                            {
                                literal.AppendByteAsChar(data.ToByteArray()[i - 1]);
                                literal.AppendByteAsChar(data.ToByteArray()[i]);
                                break;
                            }
                        }
                    else
                        literal.AppendByteAsChar(data.ToByteArray()[i - 1]);

                    break;
                }
                default:
                {
                    literal.AppendByteAsChar(data.ToByteArray()[i]);
                    break;
                }
            }

        return literal;
    }

    public static StringBuilder ToEscape(string data)
    {
        var escape = new StringBuilder(data.Length * 2);

        for (var i = 0; i < data.Length; i++)
            switch (data.ToByteArray()[i])
            {
                case 0x0:
                {
                    escape.Append('\\');
                    escape.Append('0');
                    break;
                }
                case 0x9:
                {
                    escape.Append('\\');
                    escape.Append('t');
                    break;
                }
                case 0xD:
                {
                    escape.Append('\\');
                    escape.Append('r');
                    break;
                }
                case 0xA:
                {
                    escape.Append('\\');
                    escape.Append('n');
                    break;
                }
                case 0x20:
                {
                    escape.Append('\\');
                    escape.Append('b');
                    break;
                }
                case 0x2C:
                {
                    escape.Append('\\');
                    escape.Append('c');
                    break;
                }
                case 0x5C:
                {
                    escape.Append('\\');
                    escape.Append('\\');
                    break;
                }
                default:
                {
                    escape.AppendByteAsChar(data.ToByteArray()[i]);
                    break;
                }
            }

        return escape;
    }

    // Static extensions

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

    public static bool Compare(string c1, string c2, int Length)
    {
        if (c1.Length < Length || c2.Length < Length)
            return false;
        return c1.Substring(Length) == c2.Substring(Length);
    }
}