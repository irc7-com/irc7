using System.Security.Cryptography;
using Irc.Helpers;

namespace SSPI.NTLM.Cryptography;

internal class DesEncryptor
{
    public byte[] CreateKey()
    {
        var key = new byte[8];
        do
        {
            Array.Copy(key, 0, Guid.NewGuid().ToByteArray(), 16, 8);
        } while (DES.IsWeakKey(key));

        return key;
    }

    public string Encrypt(string data, byte[] key)
    {
        SymmetricAlgorithm symmetricAlgorithm;

        symmetricAlgorithm = DES.Create();
        symmetricAlgorithm.Mode = CipherMode.ECB;
        symmetricAlgorithm.Padding = PaddingMode.None;

        using (var MS = new MemoryStream())
        {
            var bytData = data.ToByteArray();
            byte[] bytIV = { 0, 0, 0, 0, 0, 0, 0, 0 };
            var bytKey = key;
            symmetricAlgorithm.Key = bytKey;
            symmetricAlgorithm.IV = bytIV;
            var ICT = symmetricAlgorithm.CreateEncryptor();
            using (var CS = new CryptoStream(MS, ICT, CryptoStreamMode.Write))
            {
                CS.Write(bytData, 0, bytData.Length);
                CS.FlushFinalBlock();
            }

            return MS.ToArray().ToAsciiString();
        }
    }

    public byte[] Encrypt(byte[] data, byte[] key)
    {
        SymmetricAlgorithm symmetricAlgorithm;

        symmetricAlgorithm = DES.Create();
        symmetricAlgorithm.Mode = CipherMode.ECB;
        symmetricAlgorithm.Padding = PaddingMode.None;

        using (var MS = new MemoryStream())
        {
            var bytData = data;
            byte[] bytIV = { 0, 0, 0, 0, 0, 0, 0, 0 };
            var bytKey = key;
            symmetricAlgorithm.Key = bytKey;
            symmetricAlgorithm.IV = bytIV;
            var ICT = symmetricAlgorithm.CreateEncryptor();
            using (var CS = new CryptoStream(MS, ICT, CryptoStreamMode.Write))
            {
                CS.Write(bytData, 0, bytData.Length);
                CS.FlushFinalBlock();
            }

            return MS.ToArray();
        }
    }
}