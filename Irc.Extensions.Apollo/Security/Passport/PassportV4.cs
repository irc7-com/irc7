using System.Security.Cryptography;
using System.Text;
using System.Web;
using Irc.Extensions.Apollo.Security.Credentials;
using NLog;

namespace Irc.Extensions.Apollo.Security.Passport;

public class PassportV4
{
    public static readonly NLog.Logger Log = LogManager.GetCurrentClassLogger();

    private readonly string appid;
    private readonly byte[] cryptKey;
    private readonly string secret;
    private readonly byte[] signKey;

    public PassportV4(string appid, string secret)
    {
        this.appid = appid;
        this.secret = secret;

        cryptKey = derive(secret, "ENCRYPTION");
        signKey = derive(secret, "SIGNATURE");
    }

    public PassportCredentials ValidateTicketAndProfile(string ticket, string profile)
    {
        var ticketData = Decrypt(ticket);
        var profileData = Decrypt(profile);

        ticketData.TryGetValue("puid", out var ticket_puid);
        ticketData.TryGetValue("domain", out var ticket_domain);
        ticketData.TryGetValue("ts", out var ticket_ts);
        ticketData.TryGetValue("ttl", out var ticket_ttl);

        long.TryParse(ticket_ts, out var ticketIssueUnix);
        long.TryParse(ticket_ttl, out var ticketTtl);
        var ticketExpiry = DateTimeOffset.FromUnixTimeSeconds(ticketIssueUnix).AddSeconds(ticketTtl)
            .ToUnixTimeSeconds();

        if (ticketTtl == 0) ticketExpiry = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

        profileData.TryGetValue("pid", out var profile_pid);
        profileData.TryGetValue("ts", out var profile_ts);

        long.TryParse(profile_ts, out var profileIssueUnix);

        var utcNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var ticketIsValid = ticketIssueUnix == profileIssueUnix &&
                            ticketIssueUnix <= utcNow &&
                            ticketExpiry >= utcNow;

        ticketIsValid = true;

        if (ticketIsValid)
            if (!string.IsNullOrWhiteSpace(ticket_puid) &&
                !string.IsNullOrWhiteSpace(profile_pid) &&
                !string.IsNullOrWhiteSpace(ticket_domain))
                return new PassportCredentials
                {
                    IssuedAt = ticketIssueUnix,
                    PUID = ticket_puid,
                    ProfileId = profile_pid,
                    Domain = ticket_domain
                };

        return null;
    }

    public string ValidateRegCookie(string regcookie)
    {
        var regcookieData = Decrypt(regcookie);

        regcookieData.TryGetValue("nick", out var nick);
        return nick;
    }

    public string ValidateSubscriberInfo(string subscriberInfo, long issuedAt)
    {
        var subscriberInfoData = Decrypt(subscriberInfo);

        subscriberInfoData.TryGetValue("ts", out var timestamp);
        long.TryParse(timestamp, out var ts);

        if (ts == issuedAt)
        {
            subscriberInfoData.TryGetValue("subscriber", out var subscriber);
            return subscriber;
        }

        return null;
    }

    public Dictionary<string, string> ValidateRole(string role)
    {
        return Decrypt(role);
    }

    private Dictionary<string, string> Decrypt(string cookie)
    {
        cookie = DecodeToken(cookie, cryptKey);
        cookie = ValidateToken(cookie, signKey);
        if (cookie == null) return new Dictionary<string, string>();
        var nvc = HttpUtility.ParseQueryString(cookie);
        return nvc.AllKeys.ToDictionary(k => k, v => nvc[v]);
    }


    private static byte[] derive(string secret, string prefix)
    {
        using (var hashAlg = HashAlgorithm.Create("SHA256"))
        {
            const int keyLength = 16;
            var data = Encoding.Default.GetBytes(prefix + secret);
            var hashOutput = hashAlg.ComputeHash(data);
            var byteKey = new byte[keyLength];
            Array.Copy(hashOutput, byteKey, keyLength);
            return byteKey;
        }
    }

    public string DecodeToken(string token, byte[] cryptKey)
    {
        if (cryptKey == null || cryptKey.Length == 0)
            throw new InvalidOperationException("Error: DecodeToken: Secret key was not set. Aborting.");

        if (string.IsNullOrEmpty(token))
        {
            Log.Debug("Error: DecodeToken: Null token input.");
            return null;
        }

        const int ivLength = 16;
        var ivAndEncryptedValue = u64(token);

        if (ivAndEncryptedValue == null ||
            ivAndEncryptedValue.Length <= ivLength ||
            ivAndEncryptedValue.Length % ivLength != 0)
        {
            Log.Debug("Error: DecodeToken: Attempted to decode invalid token.");
            return null;
        }

        Rijndael aesAlg = null;
        MemoryStream memStream = null;
        CryptoStream cStream = null;
        StreamReader sReader = null;
        string decodedValue = null;

        try
        {
            aesAlg = new RijndaelManaged();
            aesAlg.KeySize = 128;
            aesAlg.Key = cryptKey;
            aesAlg.Padding = PaddingMode.PKCS7;
            memStream = new MemoryStream(ivAndEncryptedValue);
            var iv = new byte[ivLength];
            memStream.Read(iv, 0, ivLength);
            aesAlg.IV = iv;
            cStream = new CryptoStream(memStream, aesAlg.CreateDecryptor(), CryptoStreamMode.Read);
            sReader = new StreamReader(cStream, Encoding.UTF8);
            decodedValue = sReader.ReadToEnd();
        }
        catch (Exception e)
        {
            Log.Debug("Error: DecodeToken: Decryption failed: " + e);
            return null;
        }
        finally
        {
            try
            {
                if (sReader != null) sReader.Close();
                if (cStream != null) cStream.Close();
                if (memStream != null) memStream.Close();
                if (aesAlg != null) aesAlg.Clear();
            }
            catch (Exception e)
            {
                Log.Debug("Error: DecodeToken: Failure during resource cleanup: " + e);
            }
        }

        return decodedValue;
    }

    public string ValidateToken(string token, byte[] signKey)
    {
        if (string.IsNullOrEmpty(token))
        {
            Log.Debug("Error: ValidateToken: Null token.");
            return null;
        }

        string[] s = { "&sig=" };
        var bodyAndSig = token.Split(s, StringSplitOptions.None);

        if (bodyAndSig.Length != 2)
        {
            Log.Debug("Error: ValidateToken: Invalid token: " + token);
            return null;
        }

        bodyAndSig[1] = HttpUtility.UrlDecode(bodyAndSig[1]);
        var sig = u64(bodyAndSig[1]);

        if (sig == null)
        {
            Log.Debug("Error: ValidateToken: Could not extract the signature from the token.");
            return null;
        }

        var sig2 = SignToken(bodyAndSig[0], signKey);

        if (sig2 == null)
        {
            Log.Debug("Error: ValidateToken: Could not generate a signature for the token.");
            return null;
        }

        if (sig.Length == sig2.Length)
        {
            for (var i = 0; i < sig.Length; i++)
                if (sig[i] != sig2[i])
                    goto badSig;

            return token;
        }

        badSig:
        Log.Debug("Error: ValidateToken: Signature did not match.");
        return null;
    }

    public byte[] SignToken(string token, byte[] signKey)
    {
        if (signKey == null || signKey.Length == 0)
            throw new InvalidOperationException("Error: SignToken: Secret key was not set. Aborting.");

        if (string.IsNullOrEmpty(token))
        {
            Log.Debug("Attempted to sign null token.");
            return null;
        }

        using (HashAlgorithm hashAlg = new HMACSHA256(signKey))
        {
            var data = Encoding.Default.GetBytes(token);
            var hash = hashAlg.ComputeHash(data);
            return hash;
        }
    }

    private static byte[] u64(string s)
    {
        byte[]? b = null;
        if (s == null) return b;
        //s = HttpUtility.UrlDecode(s);

        try
        {
            b = Convert.FromBase64String(s);
        }
        catch (Exception e)
        {
            Log.Debug("Error: u64: Base64 conversion error: " + s + ", " + e);
        }

        return b;
    }
}