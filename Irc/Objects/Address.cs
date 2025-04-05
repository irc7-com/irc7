using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace Irc.Objects;

public class Address
{
    /* <nick> [ '!' <user> ] [ '@' <host> ]
       $       The  '$' prefix identifies a server on the network.
          The '$' character followed by a space or comma  may
          be used to represent the local server the client is
          connected to.
    */

    public record UserHostPair
    {
        public string User { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{User}@{Host}";
        }
    }

    public UserHostPair UserHost = new();

    public string Nickname { private set; get; } = string.Empty;

    public string User { set => UserHost.User = value; get => UserHost.User; }

    // TODO: NOTE: In Apollo, domain names are not supported in the host field; it must be a valid IP address.
    public string Host { set => UserHost.Host = value; get => UserHost.Host; }
    public string Server { set; get; } = string.Empty;

    public string RealName { set; get; } = string.Empty;
    public string RemoteIp { protected set; get; } = string.Empty;
    public string MaskedIp { protected set; get; } = string.Empty;

    public void SetNickname(string nickname)
    {
        Nickname = nickname;
    }

    public void SetIp(string address)
    {
        RemoteIp = address;
        MaskedIp = ObfuscatedAddress(address);
    }

    public string GetUserHost()
    {
        return UserHost.ToString();
    }

    public string GetAddress()
    {
        return $"{Nickname}!{User}@{Host}";
    }

    public string GetFullAddress()
    {
        return $"{Nickname}!{User}@{Host}${Server}";
    }

    public bool IsAddressPopulated()
    {
        return !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Host) &&
               !string.IsNullOrWhiteSpace(Server) && RealName != null;
    }

    public bool Parse(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return false;

        // TODO: Check for bad characters

        var regex = new Regex(
            @"((?<nick>\w+)(?:\!)(?<user>\w+)(?:\@)(?<host>\w+)(?:\$)(?<server>\w*))|((?<nick>\w+)(?:\!)(?<user>\w+)(?:\@)(?<host>\w+))|((?<user>\w+)(?:\@)(?<host>\w+))|(?<nick>\w+)");
        var match = regex.Match(address);

        if (match.Groups.Count > 0)
        {
            if (match.Groups.ContainsKey("nick")) Nickname = match.Groups["nick"].Value;
            if (match.Groups.ContainsKey("user")) User = match.Groups["user"].Value;
            if (match.Groups.ContainsKey("host")) Host = match.Groups["host"].Value;
            if (match.Groups.ContainsKey("server")) Server = match.Groups["server"].Value;
            return true;
        }

        return false;
    }

    public static string ObfuscatedAddress(string address)
    {
        using MD5 md5 = MD5.Create();
        // TODO: Temporary randomized
        byte[] encoded = Encoding.UTF8.GetBytes(address + DateTime.UtcNow.Ticks.ToString());
        byte[] hash = md5.ComputeHash(encoded);
        var hexStr = string.Concat(hash.Select(b => $"{b:x2}"));

        return hexStr.Substring(8, address.Length - 8);
    }

    public override string ToString()
    {
        return GetAddress();
    }
}