using System.Text;
using System.Text.RegularExpressions;

namespace Irc.Helpers;

public static class StringBuilderRegEx
{
    private static string sRegExEval = new('\0', 512);
    private static readonly StringBuilder sbRegExQuery = new(512);
    private static readonly StringBuilder sbRegExEval = new(512);

    public static bool EvaluateString(string query, string data, bool IgnoreCase)
    {
        sbRegExEval.Length = 0;
        sbRegExQuery.Length = 0;

        for (var i = 0; i < data.Length; i++) sbRegExEval.Append((char)data.ToByteArray()[i]);
        for (var i = 0; i < query.Length; i++)
            if (query.ToByteArray()[i] == (byte)'*')
            {
                sbRegExQuery.Append((char)46); // .
                sbRegExQuery.Append((char)42); // *
            }
            else if (query.ToByteArray()[i] == (byte)'?')
            {
                sbRegExQuery.Append((char)46); // .
            }
            else
            {
                //escape all characters to avoid screwing up regular expressions
                sbRegExQuery.Append('\\');
                sbRegExQuery.Append('x');
                byte b;
                b = (byte)(query.ToByteArray()[i] >> 4);
                sbRegExQuery.Append((char)(b > 9 ? b + 0x37 : b + 0x30));
                b = (byte)(query.ToByteArray()[i] & 0xF);
                sbRegExQuery.Append((char)(b > 9 ? b + 0x37 : b + 0x30));
            }

        var r = new Regex(sbRegExQuery.ToString(), IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        return r.Match(sbRegExEval.ToString()).Success;
    }

    public static bool EvaluteEx(string sRegularExpression, string data, bool IgnoreCase, int offset, int Length)
    {
        var r = new Regex(sRegularExpression, IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        return r.Match(data, offset, Length).Success;
    }

    public static bool EvaluateAbs(string sRegularExpression, string data, bool IgnoreCase, int offset, int Length)
    {
        var r = new Regex(sRegularExpression, IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
        return r.Match(data, offset, Length).Length == Length;
    }

    public static bool Evalute(string sRegularExpression, string data, bool IgnoreCase)
    {
        return EvaluteEx(sRegularExpression, data, IgnoreCase, 0, data.Length);
    }
}