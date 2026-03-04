using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc;

public static class Register
{
    public static void TryRegister(IChatFrame chatFrame)
    {
        if (CanRegister(chatFrame))
        {
            if (!ConnectionIsPermitted(chatFrame.Server, chatFrame.User)) return;

            chatFrame.User.Register();
            chatFrame.User.Send(Raws.IRCX_RPL_WELCOME_001(chatFrame.Server, chatFrame.User));
            chatFrame.User.Send(Raws.IRCX_RPL_WELCOME_002(chatFrame.Server, chatFrame.User,
                chatFrame.Server.ServerVersion));
            chatFrame.User.Send(Raws.IRCX_RPL_WELCOME_003(chatFrame.Server, chatFrame.User));
            chatFrame.User.Send(Raws.IRCX_RPL_WELCOME_004(chatFrame.Server, chatFrame.User,
                chatFrame.Server.ServerVersion));
            chatFrame.User.Send(Raws.IRCX_RPL_ISUPPORT_005(
                chatFrame.Server, 
                chatFrame.User,
                Resources.ConfigChannelTypes,
                "qov", // temporary
                ".@+", // temporary
                "b,k,l,SWadefghimnprstuwxz", // temporary
                chatFrame.Server.MaxChannels
                ));
            
            chatFrame.User.Send(Raws.IRCX_RPL_LUSERCLIENT_251(chatFrame.Server, chatFrame.User, 0, 0, 0));
            chatFrame.User.Send(Raws.IRCX_RPL_LUSEROP_252(chatFrame.Server, chatFrame.User, 0));
            chatFrame.User.Send(Raws.IRCX_RPL_LUSERUNKNOWN_253(chatFrame.Server, chatFrame.User, 0));
            chatFrame.User.Send(Raws.IRCX_RPL_LUSERCHANNELS_254(chatFrame.Server, chatFrame.User));
            chatFrame.User.Send(Raws.IRCX_RPL_LUSERME_255(chatFrame.Server, chatFrame.User, 0, 1));
            chatFrame.User.Send(Raws.IRCX_RPL_LUSERS_265(chatFrame.Server, chatFrame.User,
                chatFrame.Server.GetUsers().Count, 10000));
            chatFrame.User.Send(Raws.IRCX_RPL_GUSERS_266(chatFrame.Server, chatFrame.User,
                chatFrame.Server.GetUsers().Count, 10000));

            var motd = chatFrame.Server.GetMotd();
            if (motd == null)
            {
                chatFrame.User.Send(Raws.IRCX_ERR_NOMOTD_422(chatFrame.Server, chatFrame.User));
            }
            else
            {
                chatFrame.User.Send(Raws.IRCX_RPL_RPL_MOTDSTART_375(chatFrame.Server, chatFrame.User));

                foreach (var line in motd)
                    chatFrame.User.Send(Raws.IRCX_RPL_RPL_MOTD_372(chatFrame.Server, chatFrame.User, line));

                chatFrame.User.Send(Raws.IRCX_RPL_RPL_ENDOFMOTD_376(chatFrame.Server, chatFrame.User));
            }

            switch (chatFrame.User.GetLevel())
            {
                case EnumUserAccessLevel.Administrator:
                {
                    chatFrame.User.PromoteToAdministrator();
                    break;
                }
                case EnumUserAccessLevel.Sysop:
                {
                    chatFrame.User.PromoteToSysop();
                    break;
                }
                case EnumUserAccessLevel.Guide:
                {
                    chatFrame.User.PromoteToGuide();
                    break;
                }
            }
        }
    }

    public static bool ConnectionIsPermitted(IServer server, IUser user)
    {
        if (!server.AnonymousConnections && user.IsAnon())
        {
            user.Disconnect(Raws.IRCX_CLOSINGLINK(server, user, "001", "No Authorization"));
            return false;
        }

        var users = server.GetUsers();
        if (user.IsAnon())
        {
            var anonCount = users.Count(u => u.IsAnon());
            if (server.MaxAnonymousConnections > 0 && anonCount >= server.MaxAnonymousConnections)
            {
                user.Disconnect(Raws.IRCX_CLOSINGLINK(server, user, "001", "Too many anonymous connections"));
                return false;
            }
        }
        else if (user.IsGuest())
        {
            var guestCount = users.Count(u => u.IsGuest());
            if (server.MaxGuestConnections > 0 && guestCount >= server.MaxGuestConnections)
            {
                user.Disconnect(Raws.IRCX_CLOSINGLINK(server, user, "001", "Too many guest connections"));
                return false;
            }
        }
        else if (user.IsAuthenticated())
        {
            var authCount = users.Count(u => u.IsAuthenticated());
            if (server.MaxAuthenticatedConnections > 0 && authCount >= server.MaxAuthenticatedConnections)
            {
                user.Disconnect(Raws.IRCX_CLOSINGLINK(server, user, "001", "Too many authenticated connections"));
                return false;
            }
        }

        return true;
    }

    public static bool BasicAuthentication(IServer server, IUser user)
    {
        // TODO: Do basic auth
        if (!server.BasicAuthentication) return false;

        // Basic Auth would happen here

        var pass = user.Pass;
        if (!string.IsNullOrWhiteSpace(pass)) return true;

        return false;
    }

    public static bool CanRegister(IChatFrame chatFrame)
    {
        var server = chatFrame.Server;
        var user = chatFrame.User;
        var authenticating = chatFrame.User.IsAuthenticated() != true && chatFrame.User.IsAnon() == false;
        var registered = chatFrame.User.IsRegistered();
        var nickname = chatFrame.User.GetAddress().Nickname;
        var hasNickname = !string.IsNullOrWhiteSpace(nickname);
        var guest = user.IsGuest();
        var oper = user.GetLevel() >= EnumUserAccessLevel.Guide;

        if (!authenticating && !registered && hasNickname)
        {
            var isNicknameValid =
                Nick.ValidateNickname(nickname, guest, oper, authenticating, isDs: chatFrame.Server.IsDirectoryServer);

            if (!isNicknameValid)
            {
                user.Nickname = string.Empty;
                user.Send(Raws.IRCX_ERR_ERRONEOUSNICK_432(server, user, nickname));
                return false;
            }
        }

        var hasUserAddress = server.DisableUserRegistration || chatFrame.User.GetAddress().IsAddressPopulated();

        return !authenticating && !registered & hasNickname & hasUserAddress;
    }
}