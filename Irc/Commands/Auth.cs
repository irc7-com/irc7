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
    
    public IReadOnlyDictionary<string, string> SupportedPackages =>
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "GateKeeper", "GateKeeper" },
            { "NTLM",       "NTLM"       }
        };

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        if (chatFrame.User.IsRegistered())
        {
            chatFrame.User.Send(Raws.IRCX_ERR_ALREADYREGISTERED_462(chatFrame.Server, chatFrame.User));
            return;
        }
        
        if (chatFrame.User.IsAuthenticated())
        {
            chatFrame.User.Send(Raws.IRCX_ERR_ALREADYAUTHENTICATED_909(chatFrame.Server, chatFrame.User));
            return;
        }
        
        var parameters = chatFrame.ChatMessage.Parameters;
        var sequence = parameters[1].ToUpper();
        // If the sequence is invalid, return error
        if (sequence.Length > 1 || (new[] { 'I', 'S', '*' }).Contains(sequence[0]) == false)
        {
            chatFrame.User.Send(Raws.IRCX_ERR_BADCOMMAND_900(chatFrame.Server, chatFrame.User, "AUTH"));
            return;
        }
        
        var clientPackageName = parameters[0];
        // Check Passport Mode
        var usingPassport = false;
        if (clientPackageName.ToUpper().EndsWith(Resources.Passport.ToUpper()))
        {
            clientPackageName = clientPackageName.Substring(0, clientPackageName.Length - 8);
            usingPassport = true;
        }
        
        // Resolve the package and see if it is supported, if not return error
        if (!SupportedPackages.TryGetValue(clientPackageName, out var resolvedPackageName))
        {
            chatFrame.User.Send(Raws.IRCX_ERR_UNKNOWNPACKAGE_912(chatFrame.Server, chatFrame.User, clientPackageName));
            return;
        }

        var token = parameters[2].ToLiteral();
        switch (sequence)
        {
            case "I":
            {
                HandleInitialAuthMessage(chatFrame, token, ref resolvedPackageName, usingPassport);
                return;
            }
            case "S":
            {
                HandleSubsequentAuthMessage(chatFrame, token, ref resolvedPackageName, usingPassport);
                return;
            }
            case "*":
            {
                HandleAbortAuthentication(chatFrame, ref resolvedPackageName);
                return;
            }
        }
    }
    
    // The 'I' value is specified for the initial AUTH message
    private static bool HandleInitialAuthMessage(IChatFrame chatFrame, string token, ref string packageName, bool usingPassport)
    {
        var saslHandler = chatFrame.User.InitializeSspiHandler(usingPassport);
        var outgoingPackageName = usingPassport ? $"{packageName}Passport" : packageName;
        
        var supportPackageSequence =
            saslHandler.InitializeSecurityContext(packageName, token, chatFrame.Server.RemoteIp);

        if (supportPackageSequence == EnumSupportPackageSequence.SSP_OK || supportPackageSequence == EnumSupportPackageSequence.SSP_EXT)
        {
            var authResponse = saslHandler.GetAuthResponse();
            var securityTokenEscaped = authResponse.ToEscape();
            chatFrame.User.Send(Raws.RPL_AUTH_SEC_REPLY(outgoingPackageName, securityTokenEscaped));
            // Send reply
            return true;
        }

        chatFrame.User.Send(
            Raws.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, packageName));
        return false;
    }
    
    // the 'S' value is specified for all subsequent AUTH messages
    private bool HandleSubsequentAuthMessage(IChatFrame chatFrame, string token, ref string packageName, bool usingPassport)
    {
        var outgoingPackageName = usingPassport ? $"{packageName}Passport" : packageName;
        
        // Ironically not checking package name here is how MSN worked
        var saslHandler = chatFrame.User.GetSspiHandler();
        if (saslHandler == null)
        {
            chatFrame.User.Disconnect(
                Raws.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, outgoingPackageName));
            return true;
        }
        
        if (saslHandler.PendingPassportCreds)
        {
            var ticket = ExtractCookie(token);
            if (string.IsNullOrWhiteSpace(ticket))
            {
                // auth failed
                chatFrame.User.Disconnect(
                    Raws.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, outgoingPackageName));
                return true;
            }

            var profile = ExtractCookie(token.Substring(8 + ticket.Length));
            if (string.IsNullOrWhiteSpace(profile))
            {
                // auth failed
                chatFrame.User.Disconnect(
                    Raws.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, outgoingPackageName));
                return true;
            }

            if (!saslHandler.ValidatePassportCredentials(outgoingPackageName, ticket, profile))
            {
                // auth failed
                chatFrame.User.Disconnect(
                    Raws.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, outgoingPackageName));
                return true;
            }
                    
            // Finally auth the user
            AuthenticateUser(chatFrame, saslHandler, outgoingPackageName);
            return true;
        }
        
        var supportPackageSequence =
            saslHandler.AcceptSecurityContext(packageName, token, chatFrame.Server.RemoteIp);

        // Passport sequence block
        if (supportPackageSequence == EnumSupportPackageSequence.SSP_OK && 
            saslHandler.RequiresPassport)
        {
            saslHandler.PassportProvider = new PassportProvider(((Server)chatFrame.Server).Passport);
            saslHandler.PendingPassportCreds = true;
            chatFrame.User.Send(Raws.RPL_AUTH_SEC_REPLY(outgoingPackageName, "OK"));
            return true;
        }
                
        if (supportPackageSequence == EnumSupportPackageSequence.SSP_OK)
        {
            AuthenticateUser(chatFrame, saslHandler, outgoingPackageName);
            return true;
        }

        chatFrame.User.Send(
            Raws.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, outgoingPackageName));
        return false;
    }

    // If the client specifies '*' for the sequence, the server will abort
    // the authentication sequence and return IRCERR_AUTHENTICATIONFAILED
    private static void HandleAbortAuthentication(IChatFrame chatFrame, ref string packageName)
    {
        chatFrame.User.GetSspiHandler()?.Reset();
        chatFrame.User.Send(
            Raws.IRCX_ERR_AUTHENTICATIONFAILED_910(chatFrame.Server, chatFrame.User, packageName));
        return;
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