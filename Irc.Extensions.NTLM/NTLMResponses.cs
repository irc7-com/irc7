using System.Security.Cryptography;
using System.Text;
using Irc.Extensions.NTLM.Cryptography;
using Irc.Helpers;

namespace Irc.Extensions.NTLM;

public class NtlmResponses
{
    public static int LmMaxPasswordLength = 14;
    private readonly string NTLM_DES_CONST = new("KGS!@#$%");

    #region Ntlm2SessionResponse

    public string Ntlm2SessionResponse(string unicodePassword, string challenge, string clientNonce)
    {
        // Concat challenge & 8byte client nonce (lmResponse)
        var sessionNonce = $"{challenge}{clientNonce.Substring(0, 8)}";

        // MD5 to get md5Nonce
        var md5 = MD5.Create();
        var md5Nonce = md5.ComputeHash(sessionNonce.ToByteArray());

        // Truncate to 8 bytes to obtain NTLM2 session hash
        var truncatedNonce = md5Nonce.ToAsciiString().Substring(0, 8);

        // Create MD4 hash of password
        var md4 = new MD4();
        md4.HashCore(unicodePassword.ToByteArray(), 0, unicodePassword.Length);
        var md4PasswordHash = md4.HashFinal();

        return CalculateLMHash(md4PasswordHash.ToAsciiString(), truncatedNonce);
    }

    #endregion

    #region NTLMResponse

    public string NtlmResponse(string unicodePassword, string challenge)
    {
        if (string.IsNullOrWhiteSpace(unicodePassword)) throw new ArgumentException("Password cannot be empty");
        if (string.IsNullOrWhiteSpace(challenge)) throw new ArgumentException("Challenge cannot be empty");

        // Create MD4 hash of password
        var md4 = new MD4();
        md4.HashCore(unicodePassword.ToByteArray(), 0, unicodePassword.Length);
        var md4PasswordHash = md4.HashFinal();

        return CalculateLMHash(md4PasswordHash.ToAsciiString(), challenge);
    }

    #endregion

    #region NTLMv2Response (aka NTLMv2UserSessionKey)

    /*
     * The below implementation for NTLM2 is based on the below code available at Microsoft:
     * URL: https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-nlmp/5e550938-91d4-459f-b67d-75d70009e3f3
     * Subsection: 3.3.2 NTLM v2 Authentication
     *
       Define NTOWFv2(Passwd, User, UserDom) as HMAC_MD5( 
         MD4(UNICODE(Passwd)), UNICODE(ConcatenationOf( Uppercase(User), UserDom ) ) )
         EndDefine
          
         Define LMOWFv2(Passwd, User, UserDom) as NTOWFv2(Passwd, User, UserDom)
         EndDefine
          
         Set ResponseKeyNT to NTOWFv2(Passwd, User, UserDom)
         Set ResponseKeyLM to LMOWFv2(Passwd, User, UserDom)
          
         Define ComputeResponse(NegFlg, ResponseKeyNT, ResponseKeyLM, CHALLENGE_MESSAGE.ServerChallenge, ClientChallenge, Time, ServerName)
         As
         If (User is set to "" && Passwd is set to "")
             -- Special case for anonymous authentication
             Set NtChallengeResponseLen to 0
             Set NtChallengeResponseMaxLen to 0
             Set NtChallengeResponseBufferOffset to 0
             Set LmChallengeResponse to Z(1)
         Else
             Set temp to ConcatenationOf(Responserversion, HiResponserversion, Z(6), Time, ClientChallenge, Z(4), ServerName, Z(4))
             Set NTProofStr to HMAC_MD5(ResponseKeyNT, ConcatenationOf(CHALLENGE_MESSAGE.ServerChallenge,temp))
             Set NtChallengeResponse to ConcatenationOf(NTProofStr, temp)
             Set LmChallengeResponse to ConcatenationOf(HMAC_MD5(ResponseKeyLM, ConcatenationOf(CHALLENGE_MESSAGE.ServerChallenge, ClientChallenge)), ClientChallenge )
         EndIf
          
         Set SessionBaseKey to HMAC_MD5(ResponseKeyNT, NTProofStr)
         EndDefine
     *
     */

    private byte[] NtowFv2(string unicodePassword, string unicodeUsername, string unicodeDomain)
    {
        var md4 = new MD4();
        md4.HashCore(unicodePassword.ToByteArray(), 0, unicodePassword.Length);
        var md4Password = md4.HashFinal();

        var hmacmd5 = new HMACMD5(md4Password);
        return hmacmd5.ComputeHash((unicodeUsername + unicodeDomain).ToByteArray());
    }

    public string NtlmV2Response(string unicodeUsername, string unicodePassword, string serverChallenge,
        string dataBlob)
    {
        if (string.IsNullOrWhiteSpace(unicodeUsername)) throw new ArgumentException("Username cannot be empty");
        if (string.IsNullOrWhiteSpace(unicodePassword)) throw new ArgumentException("Password cannot be empty");
        if (string.IsNullOrWhiteSpace(serverChallenge)) throw new ArgumentException("Challenge cannot be empty");
        if (string.IsNullOrWhiteSpace(dataBlob))
            throw new ArgumentException("Data Blob (NTLM Parameter) cannot be empty");

        var ntlMv2Blob = new NtlmShared.NtlMv2Blob(dataBlob);

        // 1. ResponseKeyNT
        var ResponseKeyNT = NtowFv2(unicodePassword, unicodeUsername.ToUpper(), ntlMv2Blob.ClientTarget);

        // Concat serverChallenge with clientBlob
        var combined = new StringBuilder();
        combined.Append(serverChallenge);
        combined.Append(ntlMv2Blob.BlobData);

        // HMAC it with ResponseKeyNT as key
        var hmacmd5 = new HMACMD5(ResponseKeyNT);
        var NTProof = hmacmd5.ComputeHash(combined.ToString().ToByteArray());

        return NTProof.ToAsciiString();
    }

    #endregion

    #region LMResponse

    public string LmResponse(string password, string challenge)
    {
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password cannot be empty");
        if (string.IsNullOrWhiteSpace(challenge)) throw new ArgumentException("Challenge cannot be empty");
        if (password.Length > LmMaxPasswordLength)
            throw new ArgumentException($"Password cannot more than {LmMaxPasswordLength} characters");

        //Prepare password by making it upper case and padding with 0 making it 14 bytes
        password = password.ToUpper();
        var paddedPassword = password.PadRight(14, '\0');

        // Split password into half padding with 1 character on the end to make it 8 bytes
        var key1 = paddedPassword.Substring(0, 7).PadRight(8, '\0');
        var key2 = paddedPassword.Substring(7).PadRight(8, '\0');

        // Adjust for odd-parity resulting in 8 byte sets
        var key1Bytes = CreateDesOddParityKey(key1.ToByteArray());
        var key2Bytes = CreateDesOddParityKey(key2.ToByteArray());

        /* Encrypt 8 byte passwords */
        if (DES.IsWeakKey(key1Bytes) || DES.IsWeakKey(key2Bytes))
            throw new CryptographicException("Password has resulted in a weak key");

        var desEncryptor = new DesEncryptor();
        var hash1 = desEncryptor.Encrypt(NTLM_DES_CONST, key1Bytes);
        var hash2 = desEncryptor.Encrypt(NTLM_DES_CONST, key2Bytes);

        return CalculateLMHash($"{hash1}{hash2}", challenge);
    }

    private string CalculateLMHash(string hash, string challenge)
    {
        /* Pad to 21 byte hash */
        var paddedHash = hash.PadRight(21, '\0');

        // Split into 3 hashes
        var hash1 = paddedHash.Substring(0, 7);
        var hash2 = paddedHash.Substring(7, 7);
        var hash3 = paddedHash.Substring(14, 7);

        // Adjust for odd-party resulting in 8 byte sets
        var hash1Bytes = CreateDesOddParityKey(hash1.ToByteArray());
        var hash2Bytes = CreateDesOddParityKey(hash2.ToByteArray());
        var hash3Bytes = CreateDesOddParityKey(hash3.ToByteArray());

        // Calculate 3 parts against challenge
        var desEncryptor = new DesEncryptor();
        var des1 = desEncryptor.Encrypt(challenge, hash1Bytes);
        var des2 = desEncryptor.Encrypt(challenge, hash2Bytes);
        var des3 = desEncryptor.Encrypt(challenge, hash3Bytes);

        // Concatenate 3 parts together
        return $"{des1}{des2}{des3}";
    }

    private byte[] OddParity(byte[] bytes)
    {
        for (var i = 0; i <= bytes.Length - 1; i++)
        {
            var b = bytes[i];
            var needsParity = (((b >> 7) ^ (b >> 6) ^ (b >> 5) ^
                                (b >> 4) ^ (b >> 3) ^ (b >> 2) ^
                                (b >> 1)) & 0x01) == 0;
            if (needsParity)
                bytes[i] |= 0x01;
            else
                bytes[i] &= 0xfe;
        }

        return bytes;
    }

    private byte[] CreateDesOddParityKey(byte[] keyBytes)
    {
        var material = new byte[8];
        material[0] = keyBytes[0];
        material[1] = (byte)((keyBytes[0] << 7) | ((keyBytes[1] & 0xff) >> 1));
        material[2] = (byte)((keyBytes[1] << 6) | ((keyBytes[2] & 0xff) >> 2));
        material[3] = (byte)((keyBytes[2] << 5) | ((keyBytes[3] & 0xff) >> 3));
        material[4] = (byte)((keyBytes[3] << 4) | ((keyBytes[4] & 0xff) >> 4));
        material[5] = (byte)((keyBytes[4] << 3) | ((keyBytes[5] & 0xff) >> 5));
        material[6] = (byte)((keyBytes[5] << 2) | ((keyBytes[6] & 0xff) >> 6));
        material[7] = (byte)(keyBytes[6] << 1);
        return OddParity(material);
    }

    #endregion
}