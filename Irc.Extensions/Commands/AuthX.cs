using System.Text.Json;
using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Extensions.Commands;

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

        var supportPackage = chatFrame.User.GetSupportPackage();

        var packageName = parameters[0].ToUpperInvariant();
        var nonceString = parameters[1];

        if (packageName != "GATEKEEPER" && packageName != "GATEKEEPERPASSPORT")
        {
            chatFrame.User.Send(Raw.IRCX_ERR_BADVALUE_906(chatFrame.Server, chatFrame.User,
                "Only supported on GateKeeper or GateKeeperPassport"));
            return;
        }

        byte[] challenge_bytes;

        try
        {
            var bytesInt = JsonSerializer.Deserialize<int[]>(nonceString);
            challenge_bytes = bytesInt.Select(b => (byte)b).ToArray();
        }
        catch (Exception)
        {
            chatFrame.User.Send(Raw.IRCX_ERR_BADVALUE_906(chatFrame.Server, chatFrame.User,
                "Could not deserialize nonce string"));
            return;
        }

        supportPackage = chatFrame.Server.GetSecurityManager()
            .CreatePackageInstance(packageName, chatFrame.Server.GetCredentialManager());

        chatFrame.User.SetSupportPackage(supportPackage);

        supportPackage.SetChallenge(challenge_bytes);

        var jsonReadable = challenge_bytes.Select(b => (int)b).ToArray();

        chatFrame.User.Send(Raw.IRCX_INFO(chatFrame.Server, chatFrame.User,
            $"Set {supportPackage.GetPackageName()} Challenge to: {JsonSerializer.Serialize(jsonReadable)}"));
    }
}