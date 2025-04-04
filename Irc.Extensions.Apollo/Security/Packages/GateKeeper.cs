using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Irc.Enumerations;
using Irc.Extensions.Apollo.Security.Packages;
using Irc.Helpers;
using Irc.Interfaces;
using Irc.Security;
using NLog;

// ReSharper disable once CheckNamespace
namespace Irc.Extensions.Security.Packages;

public class GateKeeper : SupportPackage, ISupportPackage
{
    public static readonly NLog.Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly string _signature = "GKSSP\0";

    // Credit to JD for discovering the below key through XOR'ing (Discovered 2017/05/04)
    private static readonly string key = "SRFMKSJANDRESKKC";
    private char[] challenge;
    private byte[] challenge_bytes;
    protected GateKeeperToken ServerToken;

    public GateKeeper()
    {
        Guest = true;
        ServerToken.Signature = _signature.ToByteArray();
        ServerSequence = EnumSupportPackageSequence.SSP_INIT;
    }

    public override SupportPackage CreateInstance(ICredentialProvider? credentialProvider)
    {
        return new GateKeeper();
    }

    public override EnumSupportPackageSequence InitializeSecurityContext(string token, string ip)
    {
        // <byte(6) signature><byte(2)??><int(4) version><int(4) stage>
        if (token.Length >= 0x10)
            if (token.StartsWith(_signature))
            {
                var clientToken = GateKeeperTokenHelper.InitializeFromBytes(token.ToByteArray());
                if ((EnumSupportPackageSequence)clientToken.Sequence == EnumSupportPackageSequence.SSP_INIT &&
                    clientToken.Version is >= 1 and <= 3)
                {
                    ServerSequence = EnumSupportPackageSequence.SSP_EXT;
                    ServerVersion = clientToken.Version;
                    return EnumSupportPackageSequence.SSP_OK;
                }
            }

        return EnumSupportPackageSequence.SSP_FAILED;
    }

    public override EnumSupportPackageSequence AcceptSecurityContext(string token, string ip)
    {
        // <byte(6) signature><byte(2)??><int(4) version><int(4) stage><byte(16) challenge response><byte(16) guid>
        if (token.Length >= 0x20)
            if (token.StartsWith(_signature))
            {
                var clientToken = GateKeeperTokenHelper.InitializeFromBytes(token.ToByteArray());
                var clientVersion = clientToken.Version;
                var clientStage = (EnumSupportPackageSequence)clientToken.Sequence;

                if (clientStage != ServerSequence || clientVersion != ServerVersion)
                    return EnumSupportPackageSequence.SSP_FAILED;

                if (clientVersion == 1 && token.Length > 0x20) return EnumSupportPackageSequence.SSP_FAILED;

                var context = token.Substring(0x10, 0x10).ToByteArray();
                if (!VerifySecurityContext(new string(challenge), context, ip, ServerVersion))
                {
                    using (var writer = new StreamWriter("gkp_failed.txt", true))
                    {
                        writer.WriteLine();
                        writer.WriteLine(DateTime.UtcNow);
                        writer.WriteLine("Challenge");
                        writer.WriteLine(JsonSerializer.Serialize(challenge_bytes.Select(b => (int)b).ToArray()));
                        writer.WriteLine("Response");
                        writer.WriteLine(JsonSerializer.Serialize(context.Select(b => (int)b).ToArray()));
                    }

                    return EnumSupportPackageSequence.SSP_FAILED;
                }

                var guid = Guid.NewGuid();
                if (token.Length >= 0x30) guid = new Guid(token.Substring(0x20, 0x10).ToByteArray());

                if (guid != Guid.Empty || Guest == false)
                {
                    ServerSequence = EnumSupportPackageSequence.SSP_AUTHENTICATED;
                    Authenticated = true;

                    _credentials = new Credential
                    {
                        Level = EnumUserAccessLevel.Member,
                        Domain = GetType().Name,
                        Username = guid.ToUnformattedString().ToUpper(),
                        Guest = this is not GateKeeperPassport
                    };

                    if (this is GateKeeperPassport) return EnumSupportPackageSequence.SSP_EXT;
                    return EnumSupportPackageSequence.SSP_OK;
                }
            }

        return EnumSupportPackageSequence.SSP_FAILED;
    }

    public void SetChallenge(byte[] new_challenge)
    {
        if (challenge_bytes == null || challenge == null)
        {
            challenge_bytes = new byte[8];
            challenge = new char[8];

            Array.Copy(new_challenge, 0, challenge_bytes, 0, 8);
            Array.Copy(challenge_bytes, 0, challenge, 0, 8);
        }
    }

    public override string CreateSecurityChallenge()
    {
        ServerToken.Sequence = (int)EnumSupportPackageSequence.SSP_SEC;
        ServerToken.Version = ServerVersion;
        SetChallenge(Guid.NewGuid().ToByteArray());
        var message = new StringBuilder(Marshal.SizeOf(ServerToken) + challenge.Length);

        var serialized = ServerToken.Serialize<GateKeeperToken>();
        if (serialized.Length != Marshal.SizeOf(ServerToken)) return string.Empty;

        message.Append(serialized.ToAsciiString());
        message.Append(challenge);
        return message.ToString();
    }

    private bool VerifySecurityContext(string challenge, byte[] context, string ip, uint version)
    {
        ip = version == 3 && ip != null ? ip : "";

        var md5 = new HMACMD5(key.ToByteArray());
        var ctx = $"{challenge}{ip}";
        var h1 = md5.ComputeHash(ctx.ToByteArray(), 0, ctx.Length);

        var bHashEqual = h1.SequenceEqual(context);
        Log.Debug($"Auth: Received = {JsonSerializer.Serialize(context.Select(b => (int)b).ToArray())}");
        Log.Debug($"Auth: Expected = {JsonSerializer.Serialize(h1.Select(b => (int)b).ToArray())}");

        return bHashEqual;
    }
}