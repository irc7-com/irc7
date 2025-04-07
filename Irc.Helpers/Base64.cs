namespace Irc.Helpers;

// Base64 implementation with charsets for PassportCookie/RegCookie
public class Base64
{
    public enum B64MapType
    {
        Default,
        MSPassport,
        MSRegCookie
    }

    // A lookup array would be much faster than a conditional function for the mappings
    //public static string Base64DefaultTableMap = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";
    //public static string Base64PassportTableMap = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!*$";
    //public static string Base64RegCookieTableMap = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789^*@";

    public static string Encode(string text, B64MapType mapType = B64MapType.Default)
    {
        var result = Convert.ToBase64String(text.ToByteArray());

        if (mapType == B64MapType.MSPassport) result = result.Replace('+', '!').Replace('/', '*').Replace('=', '$');
        if (mapType == B64MapType.MSRegCookie) result = result.Replace('+', '^').Replace('/', '*').Replace('=', '@');

        return result;
    }

    public static string Decode(string text, B64MapType mapType = B64MapType.Default)
    {
        if (mapType == B64MapType.MSPassport) text = text.Replace('!', '+').Replace('*', '/').Replace('$', '=');
        if (mapType == B64MapType.MSRegCookie) text = text.Replace('^', '+').Replace('*', '/').Replace('@', '=');

        return Convert.FromBase64String(text).ToAsciiString();
    }

    public static string B64ToMSPassportMap(string text)
    {
        return text.Replace('!', '+').Replace('*', '/').Replace('$', '=');
    }

    public static string B64ToMSRegCookieMap(string text)
    {
        return text.Replace('^', '+').Replace('*', '/').Replace('@', '=');
    }
}