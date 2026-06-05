using System.Security.Cryptography;
using System.Text;
using Irc.Constants;
using Irc.Helpers;

namespace Irc.Objects.User;

public class UserAddress : IUserAddress
{
    public UserHostPair UserHost { get; set; } = new();

    public string Nickname { private set; get; } = string.Empty;

    public string User
    {
        set => UserHost.User = value;
        get => UserHost.User;
    }

    // TODO: NOTE: In Apollo, domain names are not supported in the host field; it must be a valid IP address.
    public string Host
    {
        set => UserHost.Host = value;
        get => UserHost.Host;
    }

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

    /// <summary>
    /// Returns the full address using the raw RemoteIp instead of the Host field,
    /// with IPv6-mapped IPv4 addresses normalised (::ffff:x.x.x.x → x.x.x.x).
    /// Used for IP-based access-list matching.
    /// </summary>
    public string GetIpFullAddress()
    {
        return $"{Nickname}!{User}@{GetNormalizedIp()}${Server}";
    }

    /// <summary>
    /// Returns RemoteIp with the IPv6-mapped prefix "::ffff:" stripped so that
    /// both ::ffff:10.0.0.1 and 10.0.0.1 resolve to the same string.
    /// </summary>
    public string GetNormalizedIp()
    {
        const string ipv4MappedPrefix = "::ffff:";
        if (RemoteIp.StartsWith(ipv4MappedPrefix, StringComparison.OrdinalIgnoreCase))
            return RemoteIp.Substring(ipv4MappedPrefix.Length);
        return RemoteIp;
    }

    public bool IsAddressPopulated()
    {
        return !string.IsNullOrWhiteSpace(User) && !string.IsNullOrWhiteSpace(Host) &&
               !string.IsNullOrWhiteSpace(Server) && RealName != null;
    }

    public static bool Parse(string address, out UserAddress? parsedAddress)
    {
        parsedAddress = null;
        if (string.IsNullOrWhiteSpace(address)) return false;

        var nickname = string.Empty;
        var userhost = string.Empty;
        var hostname =  string.Empty;
        var server = string.Empty;
        
        // Count occurrences — reject if more than one
        int bangCount = address.Count(c => c == '!');
        int atCount = address.Count(c => c == '@');
        int dollarCount = address.Count(c => c == '$');

        if (bangCount > 1 || atCount > 1 || dollarCount > 1)
            return false;

        int bang = address.IndexOf('!');
        int at = address.IndexOf('@');
        int dollar = address.IndexOf('$');

        // Enforce ordering
        if (bang != -1 && at != -1 && bang > at)
            return false;

        if (at != -1 && dollar != -1 && at > dollar)
            return false;

        // nickname
        int firstSep = new[] { bang, at, dollar }
            .Where(i => i != -1)
            .DefaultIfEmpty(address.Length)
            .Min();

        nickname = address[..firstSep];

        // userhost
        if (bang != -1)
        {
            int start = bang + 1;
            int end = (at != -1) ? at : (dollar != -1 ? dollar : address.Length);
            if (start > end) return false;
            
            userhost = address[start..end];
        }

        // hostname
        if (at != -1)
        {
            int start = at + 1;
            int end = (dollar != -1) ? dollar : address.Length;
            hostname = address[start..end];
        }

        // server
        if (dollar != -1)
        {
            server = address[(dollar + 1)..];
        }

        // Explicitly reject "!@$"
        if (string.IsNullOrWhiteSpace(nickname) &&
            string.IsNullOrWhiteSpace(userhost) &&
            string.IsNullOrWhiteSpace(hostname) &&
            string.IsNullOrWhiteSpace(server)) return false;
        
        // NICK!USERHOST@HOSTNAME$SERVER
        var outputParsedAddress = new UserAddress();
        outputParsedAddress.Nickname = string.IsNullOrWhiteSpace(nickname) ? Resources.Wildcard : nickname;
        outputParsedAddress.User = string.IsNullOrWhiteSpace(userhost) ? Resources.Wildcard : userhost;
        outputParsedAddress.Host = string.IsNullOrWhiteSpace(hostname) ? Resources.Wildcard : hostname;
        outputParsedAddress.Server = string.IsNullOrWhiteSpace(server) ? Resources.Wildcard : server;
        
        parsedAddress = outputParsedAddress;
        
        return true;
    }

    public static string ObfuscatedAddress(string address)
    {
        using var md5 = MD5.Create();
        // TODO: Temporary randomized
        var encoded = Encoding.UTF8.GetBytes(address + DateTime.UtcNow.Ticks);
        var hash = md5.ComputeHash(encoded);
        var hexStr = string.Concat(hash.Select(b => $"{b:x2}"));

        return hexStr.Substring(8, address.Length - 8);
    }

    public static bool Matches(IUserAddress address, string mask)
    {
        var fullAddress   = address.GetFullAddress();    // nick!user@host$server
        var ipFullAddress = address.GetIpFullAddress();  // nick!user@ip$server  (normalised)
        
        var match = Tools.MatchesMask(fullAddress, mask) ||
                    Tools.MatchesMask(ipFullAddress, mask);

        return match;
    }

    public override string ToString()
    {
        return GetAddress();
    }
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
}