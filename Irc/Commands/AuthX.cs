using System.Text.Json;
using Irc;
using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;

public class AuthX : Command, ICommand
{
    public AuthX() : base(2, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var parameters = chatFrame.Message.Parameters;

        var packageName = parameters[0].ToUpperInvariant();
        var nonceString = parameters[1];

        if (packageName != "GATEKEEPER" && packageName != "GATEKEEPERPASSPORT")
        {
            chatFrame.User.Send(Raw.IRCX_ERR_BADVALUE_906(chatFrame.Server, chatFrame.User,
                "Only supported on GateKeeper or GateKeeperPassport"));
            return;
        }

        byte[] challengeBytes;

        try
        {
            var bytesInt = JsonSerializer.Deserialize<int[]>(nonceString);
            if (bytesInt == null) throw new JsonException();

            challengeBytes = bytesInt.Select(b => (byte)b).ToArray();
        }
        catch (Exception)
        {
            chatFrame.User.Send(Raw.IRCX_ERR_BADVALUE_906(chatFrame.Server, chatFrame.User,
                "Could not deserialize nonce string"));
            return;
        }

        var credentialManager = chatFrame.Server.GetCredentialManager();
        if (credentialManager == null) throw new ArgumentNullException(nameof(credentialManager));

        var supportPackage = chatFrame.Server.GetSecurityManager()
            .CreatePackageInstance(packageName, credentialManager);

        chatFrame.User.SetSupportPackage(supportPackage);

        supportPackage.SetChallenge(challengeBytes);

        var jsonReadable = challengeBytes.Select(b => (int)b).ToArray();

        chatFrame.User.Send(Raw.IRCX_INFO(chatFrame.Server, chatFrame.User,
            $"Set {supportPackage.GetPackageName()} Challenge to: {JsonSerializer.Serialize(jsonReadable)}"));
    }
}