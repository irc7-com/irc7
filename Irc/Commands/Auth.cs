using System.Globalization;
using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;
using Irc.Objects.Server;
using Irc.Security.Credentials;

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
            chatFrame.User.Send(Raws.IRCX_ERR_ALREADYREGISTERED_462(chatFrame.Server, chatFrame.User));
        }
        else if (chatFrame.User.IsAuthenticated())
        {
            chatFrame.User.Send(Raws.IRCX_ERR_ALREADYAUTHENTICATED_909(chatFrame.Server, chatFrame.User));
        }
        else
        {
            var parameters = chatFrame.ChatMessage.Parameters;

            var packageName = parameters[0];
            var sequence = parameters[1].ToUpper();
            var token = parameters[2].ToLiteral();

            if (sequence == "I")
            {
                var saslHandler = chatFrame.User.InitializeSspiHandler();
                var packages = saslHandler.SupportedPackages;
                
                // Filter out Passport, but set it if suffix
                if (packageName.ToUpper().EndsWith("PASSPORT"))
                {
                    packageName = packageName.Substring(0, packageName.Length - 8);
                    saslHandler.RequiresPassport = true;
                }
                
                if (!packages.Contains(packageName))
                {
                    chatFrame.User.Send(Raws.IRCX_ERR_UNKNOWNPACKAGE_912(chatFrame.Server, chatFrame.User,
                        packageName));
                    return;
                }

                var supportPackageSequence =
                    saslHandler.InitializeSecurityContext(packageName, token, chatFrame.Server.RemoteIp);

                if (supportPackageSequence == EnumSupportPackageSequence.SSP_OK || supportPackageSequence == EnumSupportPackageSequence.SSP_EXT)
                {
                    var authResponse = saslHandler.GetAuthResponse();
                    var securityTokenEscaped = authResponse.ToEscape();
                    chatFrame.User.Send(Raws.RPL_AUTH_SEC_REPLY(packageName, securityTokenEscaped));
                    // Send reply
                    return;
                }
            }
            else if (sequence == "S")
            {
                // Ironically not checking package name here is how MSN worked
                var saslHandler = chatFrame.User.GetSspiHandler();
                if (saslHandler == null)
                {
                    chatFrame.User.Disconnect(
                        Raws.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, packageName));
                    return;
                }

                if (saslHandler.PendingPassportCreds)
                {
                    var ticket = ExtractCookie(token);
                    if (string.IsNullOrWhiteSpace(ticket))
                    {
                        // auth failed
                        chatFrame.User.Disconnect(
                            Raws.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, packageName));
                        return;
                    }

                    var profile = ExtractCookie(token.Substring(8 + ticket.Length));
                    if (string.IsNullOrWhiteSpace(profile))
                    {
                        // auth failed
                        chatFrame.User.Disconnect(
                            Raws.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, packageName));
                        return;
                    }

                    var credentials = saslHandler.PassportProvider.ValidateTokens(
                        new Dictionary<string, string>
                        {
                            { "ticket", ticket },
                            { "profile", profile }
                        });

                    if (credentials == null)
                    {
                        // auth failed
                        chatFrame.User.Disconnect(
                            Raws.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, packageName));
                        return;
                    }
                    
                    // If OK
                    // TODO: Need to find a better way of doing this because packageName could be anything
                    var previousPermissions = saslHandler.GetCredentials().PermissionProfile;
                    
                    // Overwrite new permissions with previous if previous was more elevated
                    if (previousPermissions.Level > credentials.PermissionProfile.Level)
                    {
                        credentials.PermissionProfile = previousPermissions;
                    }
                    saslHandler.SetCredentials(credentials);
                    
                    // Finally auth the user
                    AuthenticateUser(chatFrame, saslHandler, packageName);
                    return;
                }
                
                var supportPackageSequence =
                    saslHandler.AcceptSecurityContext(packageName, token, chatFrame.Server.RemoteIp);

                // Passport sequence block
                if (supportPackageSequence == EnumSupportPackageSequence.SSP_OK && 
                    saslHandler.RequiresPassport)
                {
                    saslHandler.PassportProvider = new PassportProvider(((Server)chatFrame.Server).Passport);
                    saslHandler.PendingPassportCreds = true;
                    chatFrame.User.Send(Raws.RPL_AUTH_SEC_REPLY(packageName, "OK"));
                    return;
                }
                
                if (supportPackageSequence == EnumSupportPackageSequence.SSP_OK)
                {
                    AuthenticateUser(chatFrame, saslHandler, packageName);
                    return;
                }
            }

            // auth failed
            chatFrame.User.Disconnect(
                Raws.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, packageName));
        }
    }
    
    private string ExtractCookie(string cookie)
    {
        if (cookie.Length < 8) return string.Empty;

        int.TryParse(cookie.Substring(0, 8), NumberStyles.HexNumber, null, out var cookieLen);

        if (cookie.Length < 8 + cookieLen) return string.Empty;

        return cookie.Substring(8, cookieLen);
    }

    private static void AuthenticateUser(IChatFrame chatFrame, ISaslHandler saslHandler, string packageName)
    {
        chatFrame.User.Authenticate();

        var credentials = saslHandler.GetCredentials();
        if (credentials == null)
        {
            // TODO: Invalid credentials handle
            chatFrame.User.Disconnect("Invalid Credentials");
            return;
        }

        var user = credentials.GetUsername();
        var domain = credentials.GetDomain();
        var userAddress = chatFrame.User.GetAddress();
        userAddress.User = string.IsNullOrWhiteSpace(user) ? userAddress.MaskedIp : user;
        userAddress.Host = credentials.GetDomain();
        userAddress.Server = chatFrame.Server.Name;
        var nickname = credentials.GetNickname();
        if (!string.IsNullOrWhiteSpace(nickname)) chatFrame.User.Name = credentials.GetNickname();
        if (credentials.Guest && string.IsNullOrWhiteSpace(chatFrame.User.GetAddress().RealName))
            userAddress.RealName = string.Empty;

        chatFrame.User.SetGuest(credentials.Guest);
        chatFrame.User.SetLevel(credentials.GetLevel());

        // TODO: find another way to work in Utf8 nicknames
        if (chatFrame.User.GetLevel() >= EnumUserAccessLevel.Guide) chatFrame.User.Utf8 = true;

        // Send reply
        chatFrame.User.Send(Raws.RPL_AUTH_SUCCESS(packageName, $"{user}@{domain}", 0));

        return;
    }
}