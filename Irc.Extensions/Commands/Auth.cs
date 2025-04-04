using Irc.Enumerations;
using Irc.Extensions.Security;
using Irc.Extensions.Security.Packages;
using Irc.Helpers;
using Irc.Interfaces;

namespace Irc.Commands;

public class Auth : Command, ICommand
{
    public Auth() : base(3, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        if (chatFrame.User.IsRegistered())
        {
            chatFrame.User.Send(Raw.IRCX_ERR_ALREADYREGISTERED_462(chatFrame.Server, chatFrame.User));
        }
        else if (chatFrame.User.IsAuthenticated())
        {
            chatFrame.User.Send(Raw.IRCX_ERR_ALREADYAUTHENTICATED_909(chatFrame.Server, chatFrame.User));
        }
        else
        {
            var parameters = chatFrame.Message.Parameters;

            var supportPackage = chatFrame.User.GetSupportPackage();
            var packageName = parameters[0];
            var sequence = parameters[1].ToUpper();
            var token = parameters[2].ToLiteral();

            if (sequence == "I")
            {
                var targetPackage = chatFrame.Server.GetSecurityManager()
                    .CreatePackageInstance(packageName, chatFrame.Server.GetCredentialManager());

                if (targetPackage == null)
                {
                    chatFrame.User.Send(Raw.IRCX_ERR_UNKNOWNPACKAGE_912(chatFrame.Server, chatFrame.User, packageName));
                    return;
                }

                if (supportPackage == null || supportPackage is ANON)
                {
                    supportPackage = chatFrame.Server.GetSecurityManager()
                        .CreatePackageInstance(packageName, chatFrame.Server.GetCredentialManager());

                    chatFrame.User.SetSupportPackage(supportPackage);
                }

                var supportPackageSequence =
                    supportPackage.InitializeSecurityContext(token, chatFrame.Server.RemoteIP);

                if (supportPackageSequence == EnumSupportPackageSequence.SSP_OK)
                {
                    var securityToken = supportPackage.CreateSecurityChallenge();

                    // If the security token could not be created, disconnect the user
                    if (securityToken == string.Empty)
                    {
                        chatFrame.User.Disconnect(Raw.IRCX_ERR_RESOURCE_907(chatFrame.Server, chatFrame.User));
                        return;
                    }

                    var securityTokenEscaped = securityToken.ToEscape();
                    chatFrame.User.Send(Raw.RPL_AUTH_SEC_REPLY(packageName, securityTokenEscaped));
                    // Send reply
                    return;
                }
            }
            else if (sequence == "S")
            {
                var supportPackageSequence =
                    chatFrame.User.GetSupportPackage().AcceptSecurityContext(token, chatFrame.Server.RemoteIP);
                if (supportPackageSequence == EnumSupportPackageSequence.SSP_OK)
                {
                    chatFrame.User.Authenticate();

                    var credentials = chatFrame.User.GetSupportPackage().GetCredentials();
                    if (credentials == null)
                    {
                        // Invalid credentials handle
                    }
                    else
                    {
                        var user = chatFrame.User.GetSupportPackage().GetCredentials().GetUsername();
                        var domain = chatFrame.User.GetSupportPackage().GetCredentials().GetDomain();
                        var userAddress = chatFrame.User.GetAddress();
                        userAddress.User = credentials.GetUsername() ?? userAddress.MaskedIP;
                        userAddress.Host = credentials.GetDomain();
                        userAddress.Server = chatFrame.Server.Name;
                        var nickname = credentials.GetNickname();
                        if (nickname != null) chatFrame.User.Name = credentials.GetNickname();
                        if (credentials.Guest && chatFrame.User.GetAddress().RealName == null)
                            userAddress.RealName = string.Empty;

                        chatFrame.User.SetGuest(credentials.Guest);
                        chatFrame.User.SetLevel(credentials.GetLevel());

                        // TODO: find another way to work in Utf8 nicknames
                        if (chatFrame.User.GetLevel() >= EnumUserAccessLevel.Guide) chatFrame.User.Utf8 = true;

                        // Send reply
                        chatFrame.User.Send(Raw.RPL_AUTH_SUCCESS(packageName, $"{user}@{domain}", 0));
                    }

                    return;
                }

                if (supportPackageSequence == EnumSupportPackageSequence.SSP_CREDENTIALS)
                {
                    chatFrame.User.Send(Raw.RPL_AUTH_SEC_REPLY(packageName, "OK"));
                    return;
                }
            }

            // auth failed
            chatFrame.User.Disconnect(
                Raw.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, packageName));
        }
    }
}