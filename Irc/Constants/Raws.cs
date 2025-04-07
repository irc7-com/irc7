using Irc.Interfaces;

namespace Irc.Constants;

public static class IrcRaws
{
    public static string IRC_RAW_001 = ":%h 001 %a :Welcome to the Microsoft Internet Chat Server %a";
    public static string IRC_RAW_002 = ":%h 002 %a :Your host is %h, running version %v ";
    public static string IRC_RAW_003 = ":%h 003 %a :This server was created %s at %s PDT";
    public static string IRC_RAW_004 = ":%h 004 %a %h %v aioxz abcdefhiklmnoprstuvxyz   ";
    public static string IRC_RAW_219 = ":%h 219 %a %s :End of /STATS report ";
    public static string IRC_RAW_221 = ":%h 221 %n %s   ";
    public static string IRC_RAW_251 = ":%h 251 %a :There are %d users and %d invisible on %d servers   ";
    public static string IRC_RAW_252 = ":%h 252 %a %d :operator(s) online   ";
    public static string IRC_RAW_253 = ":%h 253 %a %d :unknown connection(s)";
    public static string IRC_RAW_254 = ":%h 254 %a %d :channels formed  ";
    public static string IRC_RAW_255 = ":%h 255 %a :I have %d clients and %d servers";
    public static string IRC_RAW_301 = ":%h 301 %a %s :%s";
    public static string IRC_RAW_305 = ":%h 305 %a :You are no longer marked as being away  ";
    public static string IRC_RAW_306 = ":%h 306 %a :You have been marked as being away  ";
    public static string IRC_RAW_315 = ":%h 315 %a %s :End of /WHO list ";
    public static string IRC_RAW_321 = ":%h 321 %a Channel :Users  Name ";
    public static string IRC_RAW_322 = ":%h 322 %a %s %d :%s";
    public static string IRC_RAW_323 = ":%h 323 %a :End of /LIST";
    public static string IRC_RAW_324 = ":%h 324 %a %c %s";
    public static string IRC_RAW_331 = ":%h 331 %a %c :No topic is set  ";
    public static string IRC_RAW_332 = ":%h 332 %a %c :%s   ";
    public static string IRC_RAW_341 = ":%h 341 %a %s %s";
    public static string IRC_RAW_351 = ":%h 351 %a %d.%d.%d %h :Microsoft Exchange Chat Service %d.%d   ";
    public static string IRC_RAW_352 = ":%h 352 %a %s %s %s %s %s %s%s :%d %s   ";
    public static string IRC_RAW_366_STR = ":%h 366 %a %s :End of /NAMES list.  ";
    public static string IRC_RAW_366_ANY = ":%h 366 %a * :End of /NAMES list.   ";
    public static string IRC_RAW_366 = ":%h 366 %a %c :End of /NAMES list.  ";
    public static string IRC_RAW_367 = ":%h 367 %a %c %p";
    public static string IRC_RAW_368 = ":%h 368 %a %c :End of Channel Ban List  ";
    public static string IRC_RAW_371 = ":%h 371 %a :On-line since %s";
    public static string IRC_RAW_371_DATE = ":%h 371 %a :%s %d.%d";
    public static string IRC_RAW_372 = ":%h 372 %a :- %q";
    public static string IRC_RAW_374 = ":%h 374 %a :End of /INFO list   ";
    public static string IRC_RAW_375 = ":%h 375 %a :- %h Message of the Day -   ";
    public static string IRC_RAW_376 = ":%h 376 %a :End of /MOTD command";
    public static string IRC_RAW_381 = ":%h 381 %a :You are now an IRC operator ";
    public static string IRC_RAW_386 = ":%h 386 %a :You are now an IRC administrator";
    public static string IRC_RAW_391 = ":%h 391 %a %s :%s   ";
    public static string IRC_RAW_401 = ":%h 401 %a %s :No such nick/channel ";
    public static string IRC_RAW_402 = ":%h 402 %a %s :No such server   ";
    public static string IRC_RAW_403 = ":%h 403 %a %s :No such channel  ";
    public static string IRC_RAW_404 = ":%h 404 %a %s :Cannot send to channel   ";
    public static string IRC_RAW_405 = ":%h 405 %a %s :You have joined too many channels";
    public static string IRC_RAW_411 = ":%h 411 %a :No recipient given (%s) ";
    public static string IRC_RAW_412 = ":%h 412 %a :No text to send (%s)";
    public static string IRC_RAW_421 = ":%h 421 %a %s :Unknown command";
    public static string IRC_RAW_422 = ":%h 422 %a :MOTD File is missing";
    public static string IRC_RAW_431 = ":%h 431 :No nickname given  ";
    public static string IRC_RAW_432 = ":%h 432 %s :Erroneous nickname  ";
    public static string IRC_RAW_433 = ":%h 433 * %s :Nickname is already in use";
    public static string IRC_RAW_442 = ":%h 442 %a %s :You're not on that channel   ";
    public static string IRC_RAW_443 = ":%h 443 %a %s %s :is already on channel ";
    public static string IRC_RAW_451 = ":%h 451 %a :You have not registered ";
    public static string IRC_RAW_461 = ":%h 461 %a %s :Not enough parameters";
    public static string IRC_RAW_467 = ":%h 467 %a %c :Channel key already set  ";
    public static string IRC_RAW_471 = ":%h 471 %a %s :Cannot join channel (+l) ";
    public static string IRC_RAW_473 = ":%h 473 %a %s :Cannot join channel (+i) ";
    public static string IRC_RAW_474 = ":%h 474 %a %s :Cannot join channel (+b) ";
    public static string IRC_RAW_475 = ":%h 475 %a %s :Cannot join channel (+k) ";
    public static string IRC_RAW_482 = ":%h 482 %a %s :You're not channel operator  ";
    public static string IRC_RAW_485 = ":%h 485 %a %s :You're not channel owner ";
    public static string IRC_RAW_491 = ":%h 491 %a :No O-lines for your host";
    public static string IRC_RAW_502 = ":%h 502 %a :Cant change mode for other users";
    public static string IRC_RAW_554 = ":%h 554 %a %s :Command not supported.   ";
    public static string IRC_RAW_555 = ":%h 555 %a %s :Server option for this command is not supported. ";
    public static string IRC_RAW_557 = ":%h 557 %a :Command aborted to prevent output buffer overflow.  ";
    public static string IRC_RAW_900 = ":%h 900 %a %s :Cannot join MIC only channel with IRC client ";
    public static string IRC_RAW_901_ARG = ":%h 901 %a %s :Too many arguments";
    public static string IRC_RAW_901_REMOTE = ":%h 901 %a %s :Cannot join channel from remote server (+r)";
    public static string IRC_RAW_902 = ":%h 902 %a %s :Cannot create dynamic channels (admin)";
    public static string IRC_RAW_904 = ":%h 904 %a %s :Only authenticated users may join channel";
    public static string IRC_RAW_905 = ":%h 905 %a :Nick changes are not permitted at this time, try again later";
    public static string IRC_RAW_906 = ":%h 906 %a %s :Cannot make host due to admin restriction";
    public static string IRC_RAW_998 = ":%h 998 %a %s :Already on channel   ";

    public static string IRC_RAW_256(IServer server, IUser user)
    {
        return $":{server} 256 {user} :Administrative info about {server}";
    }

    public static string IRC_RAW_257(IServer server, IUser user, string message)
    {
        return $":{server} 257 {user} :{message}";
    }

    public static string IRC_RAW_258(IServer server, IUser user, string message)
    {
        return $":{server} 258 {user} :{message}";
    }

    public static string IRC_RAW_259(IServer server, IUser user, string email)
    {
        return $":{server} 259 {user} :{email}";
    }

    public static string IRC_RAW_303(IServer server, IUser user, string names)
    {
        return $":{server} 303 {user} :{names}";
    }

    public static string IRC_RAW_311(IServer server, IUser user, IUser targetUser)
    {
        return
            $":{server} 311 {user} {targetUser} {targetUser.GetAddress().User} {targetUser.GetAddress().Host} * :{targetUser.GetAddress().RealName}";
    }

    public static string IRC_RAW_312(IServer server, IUser user, IUser targetUser)
    {
        return $":{server} 312 {user} {targetUser} {server} :{server.Info}";
    }

    public static string IRC_RAW_313(IServer server, IUser user, IUser targetUser)
    {
        return $":{server} 313 {user} {targetUser} :is an IRC operator";
    }

    public static string IRC_RAW_317(IServer server, IUser user, IUser targetUser, long seconds, long epoch)
    {
        return $":{server} 317 {user} {targetUser} {seconds} {epoch} :seconds idle, signon time";
    }

    public static string IRC_RAW_318(IServer server, IUser user, string targetNicknames)
    {
        return $":{server} 318 {user} {targetNicknames} :End of /WHOIS list";
    }

    public static string IRC_RAW_319(IServer server, IUser user, IUser targetUser, string channelList)
    {
        return $":{server} 319 {user} {targetUser} :{channelList}";
    }

    public static string IRC_RAW_353(IServer server, IUser user, IChannel channel, string names)
    {
        return $":{server} 353 {user} = {channel} :{names}";
    }

    public static string IRC_RAW_364(IServer server, IUser user, string mask, int hopcount)
    {
        return $":{server} 364 {user} {mask} {server} :{hopcount} P0 {server.Info}";
    }

    public static string IRC_RAW_365(IServer server, IUser user, string mask)
    {
        return $":{server} 365 {user} {mask} :End of /LINKS list.";
    }

    public static string IRC_RAW_423(IServer server, IUser user)
    {
        return $":{server} 423 {user} {server} :No administrative info available ";
    }

    public static string IRC_RAW_446(IServer server, IUser user)
    {
        return $":{server} 446 {user} :USERS has been disabled";
    }

    public static string IRC_RAW_462(IServer server, IUser user)
    {
        return $":{server} 462 {user} :You may not reregister";
    }

    public static string IRC_RAW_472(IServer server, IUser user, string modeChar)
    {
        return $":{server} 472 {user} {modeChar} :is unknown mode char to me";
    }

    public static string IRC_RAW_481(IServer server, IUser user)
    {
        return $":{server} 481 {user} :Permission Denied - You're not an IRC operator";
    }

    public static string IRC_RAW_501(IServer server, IUser user)
    {
        return $":{server} 501 {user} :Unknown MODE flag";
    }

    public static string IRC_RAW_908(IServer server, IUser user)
    {
        return $":{server} 908 {user} :No permissions to perform command";
    }

    public static string IRC_RAW_999(IServer server, IUser user, string message)
    {
        return $":{server} 999 {user} :Oops! We've hit a snag: {message}";
    }

    public static string RPL_JOIN(IUser user, IChannel channel)
    {
        return $":{user.GetAddress()} JOIN :{channel}";
    }

    public static string RPL_PART(IUser user, IChannel channel)
    {
        return $":{user.GetAddress()} PART {channel}";
    }

    public static string RPL_PRIVMSG(IUser user, IChannel channel, string message)
    {
        return $":{user.GetAddress()} PRIVMSG {channel} :{message}";
    }

    public static string RPL_NOTICE(IUser user, IChannel channel, string message)
    {
        return $":{user.GetAddress()} NOTICE {channel} :{message}";
    }

    public static string RPL_QUIT(IUser user, string message)
    {
        return $":{user.GetAddress()} QUIT :{message}";
    }
}

public class IrcxRaws
{
    public static string IRCX_CLOSINGLINK = "ERROR :Closing Link: %n[%s] (%s)";

    public static string IRCX_CLOSINGLINK_007_SYSTEMKILL =
        "ERROR :Closing Link: %n[%s] 007 (Killed by system operator)";

    public static string IRCX_CLOSINGLINK_008_INPUTFLOODING = "ERROR :Closing Link: %n[%s] 008 (Input flooding)";
    public static string IRCX_CLOSINGLINK_009_OUTPUTSATURATION = "ERROR :Closing Link: %n[%s] 009 (Output Saturation)";
    public static string IRCX_CLOSINGLINK_011_PINGTIMEOUT = "ERROR :Closing Link: %n[%s] 011 (Ping timeout)";

    public static string RPL_AUTH_INIT = "AUTH %s I :%s";
    public static string RPL_AUTH_SEC_REPLY = "AUTH %s S :%s";
    public static string RPL_AUTH_SUCCESS = "AUTH %s * %s %o";
    public static string RPL_JOIN_IRC = ":%u JOIN :%c";
    public static string RPL_JOIN_MSN = ":%u JOIN %S :%c";
    public static string RPL_PART_IRC = ":%u PART %c";

    public static string RPL_MODE_IRC = ":%u MODE %s %s";
    public static string RPL_TOPIC_IRC = ":%u TOPIC %c :%s";
    public static string RPL_PROP_IRCX = ":%u PROP %c %s :%s";
    public static string RPL_KICK_IRC = ":%u KICK %c %s :%s";
    public static string RPL_KILL_IRC = ":%u KILL %s :%s";
    public static string RPL_SERVERKILL_IRC = ":%h KILL %s :%s";
    public static string RPL_KILLED = ":%s KILLED";
    public static string RPL_MSG_CHAN = ":%u %s %c :%s";
    public static string RPL_MSG_USER = ":%u %s %s :%s";
    public static string RPL_CHAN_MSG = ":%s %s %s :%s";
    public static string RPL_CHAN_WHISPER = ":%u WHISPER %c %s :%s";
    public static string RPL_EPRIVMSG_CHAN = ":%u EPRIVMSG %c :%s";
    public static string RPL_NOTICE_USER = ":%u NOTICE %s :%s";
    public static string RPL_PRIVMSG_USER = ":%u PRIVMSG %s :%s";
    public static string RPL_NOTICE_CHAN = ":%u NOTICE %c :%s";
    public static string RPL_PRIVMSG_CHAN = ":%u PRIVMSG %c :%s";
    public static string RPL_INVITE = ":%u INVITE %s %s :%c";
    public static string RPL_KNOCK_CHAN = ":%u KNOCK %c %s";
    public static string RPL_NICK = ":%u NICK %s";
    public static string RPL_PONG = "PONG %h :%n";
    public static string RPL_PONG_CLIENT = "PONG :%s";
    public static string RPL_PING = "PING :%h";
    public static string RPL_SERVICE_DATA = ":%h SERVICE %s %s"; //e.g. :SERVERNAMEs SERVICE DELETE CHANNEL %#Channel


    public static string IRCX_RPL_WELCOME_001 = ":%h 001 %n :Welcome to the %s server %n";
    public static string IRCX_RPL_WELCOME_002 = ":%h 002 %n :Your host is %h, running version %d.%d.%d";
    public static string IRCX_RPL_WELCOME_003 = ":%h 003 %n :This server was created %s";
    public static string IRCX_RPL_WELCOME_004 = ":%h 004 %n %h %d.%d.%d aioxz abcdefhiklmnoprstuvxyz";
    public static string IRCX_RPL_WELCOME_005 = ":%h 005 %n IRCX PREFIX=(qov).@+ CHANTYPES=%# CHANLIMIT=%#:1";

    public static string IRCX_RPL_UMODEIS_221 = ":%h 221 %n %s";

    public static string IRCX_RPL_LUSERCLIENT_251 = ":%h 251 %n :There are %d users and %d invisible on %d servers";
    public static string IRCX_RPL_LUSEROP_252 = ":%h 252 %n %d :operator(s) online";
    public static string IRCX_RPL_LUSERUNKNOWN_253 = ":%h 253 %n %d :unknown connection(s)";
    public static string IRCX_RPL_LUSERCHANNELS_254 = ":%h 254 %n %d :channels formed";
    public static string IRCX_RPL_LUSERME_255 = ":%h 255 %n :I have %d clients and %d servers";

    public static string IRCX_RPL_ADMINME_256 = ":%h 256 %n :Administrative info about %h";
    public static string IRCX_RPL_ADMINLOC1_257 = ":%h 257 %n :%s";
    public static string IRCX_RPL_ADMINLOC1_258 = ":%h 258 %n :%s";
    public static string IRCX_RPL_ADMINEMAIL_259 = ":%h 259 %n :%s";

    public static string IRCX_RPL_LUSERS_265 = ":%h 265 %n :Current local users: %d Max: %d";
    public static string IRCX_RPL_GUSERS_266 = ":%h 266 %n :Current global users: %d Max: %d";

    public static string IRCX_RPL_AWAY_301 = ":%h 301 %n %s :%s";
    public static string IRCX_RPL_USERHOST_302 = ":%h 302 %n :%s";
    public static string IRCX_RPL_UNAWAY_305 = ":%h 305 %n :You are no longer marked as being away";
    public static string IRCX_RPL_NOWAWAY_306 = ":%h 306 %n :You have been marked as being away";
    public static string IRCX_RPL_WHOISUSER_311 = ":%h 311 %n %s %s %s * :%s";
    public static string IRCX_RPL_WHOISSERVER_312 = ":%h 312 %n %s %s :%s";
    public static string IRCX_RPL_WHOISOPERATOR_313A = ":%h 313 %n %s :is an IRC administrator";
    public static string IRCX_RPL_WHOISOPERATOR_313O = ":%h 313 %n %s :is an IRC operator";
    public static string IRCX_RPL_ENDOFWHO_315 = ":%h 315 %n %s :End of /WHO list";
    public static string IRCX_RPL_WHOISIDLE_317 = ":%h 317 %n %s %d %d :seconds idle, signon time";
    public static string IRCX_RPL_ENDOFWHOIS_318 = ":%h 318 %n %s :End of /WHOIS list";
    public static string IRCX_RPL_WHOISCHANNELS_319 = ":%h 319 %n %s :%s";
    public static string IRCX_RPL_WHOISCHANNELS_319X = ":%h 319 %n %s :";
    public static string IRCX_RPL_WHOISIP_320 = ":%h 320 %n %s :from IP %s";
    public static string IRCX_RPL_MODE_324 = ":%h 324 %n %c %s";
    public static string IRCX_RPL_NOTOPIC_331 = ":%h 331 %n %c :No topic is set";
    public static string IRCX_RPL_TOPIC_332 = ":%h 332 %n %c :%s";
    public static string IRCX_RPL_VERSION_351 = ":%h 351 %n %d.%d.%d %h :%h %d.%d";
    public static string IRCX_RPL_WHOREPLY_352 = ":%h 352 %n %s %s %s %h %s %s%s%s :0 %s";
    public static string IRCX_RPL_NAMEREPLY_353 = ":%h 353 %n = %c :%s";
    public static string IRCX_RPL_NAMEREPLY_353X = ":%h 353 %n = %c :";
    public static string IRCX_RPL_ENDOFNAMES_366 = ":%h 366 %n %s :End of /NAMES list.";
    public static string IRCX_RPL_BANLIST_367 = ":%h 367 %n %c %s";
    public static string IRCX_RPL_ENDOFBANLIST_368 = ":%h 368 %n %c :End of Channel Ban List";
    public static string IRCX_RPL_RPL_INFO_371 = ":%h 371 %n :On-line since %s";
    public static string IRCX_RPL_RPL_INFO_371_VERS = ":%h 371 %n :%s %d.%d";
    public static string IRCX_RPL_RPL_MOTD_372 = ":%h 372 %n :- %s";
    public static string IRCX_RPL_RPL_ENDOFINFO_374 = ":%h 374 %n :End of /INFO list";
    public static string IRCX_RPL_RPL_MOTDSTART_375 = ":%h 375 %n :- %h Message of the Day -";
    public static string IRCX_RPL_RPL_ENDOFMOTD_376 = ":%h 376 %n :End of /MOTD command";

    public static string IRCX_RPL_YOUREOPER_381 = ":%h 381 %n :You are now an IRC operator";
    public static string IRCX_RPL_YOUREADMIN_386 = ":%h 386 %n :You are now an IRC administrator";
    public static string IRCX_RPL_TIME_391 = ":%h 391 %n %h :%S";

    public static string IRCX_ERR_NOSUCHNICK_401_N = ":%h 401 %n %s :No such nick";
    public static string IRCX_ERR_NOSUCHNICK_401 = ":%h 401 %n %s :No such nick/channel";
    public static string IRCX_ERR_NOSUCHCHANNEL_403 = ":%h 403 %n %s :No such channel";
    public static string IRCX_ERR_CANNOTSENDTOCHAN_404 = ":%h 404 %n %s :Cannot send to channel";
    public static string IRCX_ERR_CANNOTSENDTOCHAN_404X = ":%h 404 %n %c :Cannot send to channel";
    public static string IRCX_ERR_TOOMANYCHANNELS_405 = ":%h 405 %n %c :You have joined too many channels";
    public static string IRCX_ERR_NOORIGIN_409 = ":%h 409 %n :No origin specified";
    public static string IRC_ERR_NORECIPIENT_411 = ":%h 411 %n :No recipient given (%s)";
    public static string IRC_ERR_NOTEXT_412 = ":%h 412 %n :No text to send (%s)";
    public static string IRCX_ERR_UNKNOWNCOMMAND_421 = ":%h 421 %n %s :Unknown command";
    public static string IRCX_ERR_UNKNOWNCOMMAND_421_T = ":%h 421 %n %s :String parameter must be 160 chars or less";
    public static string IRCX_ERR_NOMOTD_422 = ":%h 422 %n :MOTD File is missing";
    public static string IRCX_ERR_ERRONEOUSNICK_432 = ":%h 432 %s :Erroneous nickname";
    public static string IRCX_ERR_NICKINUSE_433 = ":%h 433 * %s :Nickname is already in use";
    public static string IRCX_ERR_NONICKCHANGES_439 = ":%h 439 %n %s :Nick name changes not permitted.";
    public static string IRCX_ERR_NOTONCHANNEL_442 = ":%h 442 %n %s :You're not on that channel";
    public static string IRCX_ERR_USERONCHANNEL_443 = ":%h 443 %n %s %s :is already on channel";
    public static string IRCX_ERR_NOTREGISTERED_451 = ":%h 451 %n :You have not registered";
    public static string IRCX_ERR_NEEDMOREPARAMS_461 = ":%h 461 %n %s :Not enough parameters";
    public static string IRCX_ERR_ALREADYREGISTERED_462 = ":%h 462 %n :You may not reregister";
    public static string IRCX_ERR_KEYSET_467 = ":%h 467 %n %c :Channel key already set";
    public static string IRCX_ERR_CHANNELISFULL_471 = ":%h 471 %n %c :Cannot join channel (+l)";
    public static string IRCX_ERR_UNKNOWNMODE_472 = ":%h 472 %n %l :is unknown mode char to me";
    public static string IRCX_ERR_INVITEONLYCHAN_473 = ":%h 473 %n %c :Cannot join channel (+i)";
    public static string IRCX_ERR_BANNEDFROMCHAN_474 = ":%h 474 %n %c :Cannot join channel (+b)";
    public static string IRCX_ERR_BADCHANNELKEY_475 = ":%h 475 %n %c :Cannot join channel (+k)";
    public static string IRCX_ERR_NOPRIVILEGES_481 = ":%h 481 %n :Permission Denied - You're not an IRC operator";
    public static string IRCX_ERR_CHANOPRIVSNEEDED_482 = ":%h 482 %n %c :You're not channel operator";
    public static string IRCX_ERR_CHANQPRIVSNEEDED_485 = ":%h 485 %n %c :You're not channel owner";

    public static string IRCX_ERR_USERSDONTMATCH_502 = ":%h 502 %n :Cant change mode for other users";

    public static string IRCX_ERR_OPTIONUNSUPPORTED_555 =
        ":%h 555 %n %s :Server option for this command is not supported.";

    public static string IRCX_ERR_AUTHONLYCHAN_556 = ":%h 556 %n %c :Only authenticated users may join channel.";
    public static string IRCX_ERR_SECUREONLYCHAN_557 = ":%h 557 %n %c :Only secure users may join channel.";

    public static string IRCX_RPL_FINDS_613 = ":%h 613 %n :%s %s";
    public static string IRCX_RPL_LISTRSTART_614 = ":%h 811 %n :Start of ListR";
    public static string IRCX_RPL_LISTRLIST_614 = ":%h 812 %n %c %d %s :%s";
    public static string IRCX_RPL_LISTREND_614 = ":%h 817 %n :End of ListR";
    public static string IRCX_RPL_YOUREGUIDE_629 = ":%h 629 %n :You are now an IRC guide";

    public static string IRC2_RPL_WHOISSECURE_671 = ":%h 671 %n %s :is using a secure connection";

    public static string IRCX_RPL_FINDS_NOSUCHCAT_701 = ":%h 701 %n :Category not found";
    public static string IRCX_RPL_FINDS_NOTFOUND_702 = ":%h 702 %n :Channel not found";
    public static string IRCX_RPL_FINDS_DOWN_703 = ":%h 703 %n :Server down. Retry later.";
    public static string IRCX_RPL_FINDS_CHANNELEXISTS_705 = ":%h 705 %n :Channel with same name exists";
    public static string IRCX_RPL_FINDS_INVALIDCHANNEL_706 = ":%h 706 %n :Channel name is not valid";

    public static string IRCX_RPL_IRCX_800 = ":%h 800 %n %d %d %s %d %s";
    public static string IRCX_RPL_ACCESSADD_801 = ":%h 801 %n %s %s %s %d %s :%s";
    public static string IRCX_RPL_ACCESSDELETE_802 = ":%h 802 %n %s %s %s %d %s :%s";
    public static string IRCX_RPL_ACCESSSTART_803 = ":%h 803 %n %s :Start of access entries";
    public static string IRCX_RPL_ACCESSLIST_804 = ":%h 804 %n %s %s %s %d %s :%s";
    public static string IRCX_RPL_ACCESSEND_805 = ":%h 805 %n %s :End of access entries";
    public static string IRCX_RPL_EVENTADD_806 = ":%h 806 %n %s %s";
    public static string IRCX_RPL_EVENTDEL_807 = ":%h 807 %n %s %s";
    public static string IRCX_RPL_EVENTSTART_808 = ":%h 808 %n %s :Start of events";
    public static string IRCX_RPL_EVENTLIST_809 = ":%h 809 %n %s %s";
    public static string IRCX_RPL_EVENTEND_810 = ":%h 810 %n %s :End of events";
    public static string IRCX_RPL_LISTXSTART_811 = ":%h 811 %n :Start of ListX";
    public static string IRCX_RPL_LISTXLIST_812 = ":%h 812 %n %s %s %d %d :%s";
    public static string IRCX_RPL_LISTXPICS_813 = ":%h 813 %n :%s";
    public static string IRCX_RPL_LISTXTRUNC_816 = ":%h 816 %n :Truncation of ListX";
    public static string IRCX_RPL_LISTXEND_817 = ":%h 817 %n :End of ListX";

    public static string IRCX_RPL_PROPLIST_818 = ":%h 818 %n %s %s :%s";
    public static string IRCX_RPL_PROPEND_819 = ":%h 819 %n %s :End of properties";
    public static string IRCX_RPL_ACCESSCLEAR_820 = ":%h 820 %n %s %s :Clear";
    public static string IRCX_RPL_USERUNAWAY_821 = ":%u 821 :User unaway";
    public static string IRCX_RPL_USERNOWAWAY_822 = ":%u 822 :%s";

    public static string IRCX_RPL_REVEAL_851 = ":%h 851 %n %s %s %s %s :%s";
    public static string IRCX_RPL_REVEALEND_852 = ":%h 852 %n :End of ListX";

    public static string IRCX_ERR_BADCOMMAND_900 = ":%h 900 %n %s :Bad command";
    public static string IRCX_ERR_TOOMANYARGUMENTS_901 = ":%h 901 %n %s :Too many arguments";
    public static string IRCX_ERR_BADLYFORMEDPARAMS_902 = ":%h 902 %n :Badly formed parameters";
    public static string IRCX_ERR_BADLEVEL_903 = ":%h 903 %n %s :Bad level";
    public static string IRCX_ERR_BADPROPERTY_905 = ":%h 905 %n %s :Bad property specified";
    public static string IRCX_ERR_BADVALUE_906 = ":%h 906 %n %s :Bad value specified";
    public static string IRCX_ERR_RESOURCE_907 = ":%h 907 %n :Not enough resources";
    public static string IRCX_ERR_SECURITY_908 = ":%h 908 %n :No permissions to perform command";
    public static string IRCX_ERR_ALREADYAUTHENTICATED_909 = ":%h 909 %n %s :Already authenticated";
    public static string IRCX_ERR_AUTHENTICATIONFAILED_910 = ":%h 910 %n %s :Authentication failed";
    public static string IRCX_ERR_AUTHENTICATIONSUSPENDED_911 = ":%h 911 %n %s :Authentication suspended for this IP";
    public static string IRCX_ERR_UNKNOWNPACKAGE_912 = ":%h 912 %n %s :Unsupported authentication package";
    public static string IRCX_ERR_NOACCESS_913 = ":%h 913 %n %s :No access";
    public static string IRCX_ERR_DUPACCESS_914 = ":%h 914 %n :Duplicate access entry";
    public static string IRCX_ERR_MISACCESS_915 = ":%h 915 %n :Unknown access entry";
    public static string IRCX_ERR_TOOMANYACCESSES_916 = ":%h 916 %n :Too many access entries";
    public static string IRCX_ERR_EVENTDUP_918 = ":%h 918 %n %s %s :Duplicate event entry";
    public static string IRCX_ERR_EVENTMIS_919 = ":%h 919 %n %s %s :Unknown event entry";
    public static string IRCX_ERR_NOSUCHEVENT_920 = ":%h 920 %n %s :No such event";
    public static string IRCX_ERR_TOOMANYEVENTS_921 = ":%h 921 %n %s :Too many events specified";
    public static string IRCX_ERR_ACCESSNOTCLEAR_922 = ":%h 922 %n :Some entries not cleared due to security";
    public static string IRCX_ERR_NOWHISPER_923 = ":%h 923 %n %c :Does not permit whispers";
    public static string IRCX_ERR_NOSUCHOBJECT_924 = ":%h 924 %n %s :No such object found";
    public static string IRCX_ERR_ALREADYONCHANNEL_927 = ":%h 927 %n %s :Already in the channel.";
    public static string IRCX_ERR_ALREADYONCHANNEL_927X = ":%h 927 %n %c :Already in the channel.";
    public static string IRCX_ERR_U_NOTINCHANNEL_928 = "%h 928 %n :You're not in a channel";
    public static string IRCX_ERR_TOOMANYINVITES_929 = ":%h 929 %n %s %s :Cannot invite. Too many invites.";
    public static string IRCX_ERR_NOTIMPLEMENTED = ":%h 999 %n :%s Sorry, this command is not implemented yet.";

    public static string IRCX_ERR_EXCEPTION =
        ":%h 999 %n :%s Oops! Looks like you've hit a snag here, please can you kindly report this.";

    // DIRECTORY SERVER SPECIFIC
    public static string MSN_FOUND_CHAN = ":%h 613 %n :%s %s";
    public static string MSN_FOUND_USER_HEAD = ":%h 641 %n :%s";
    public static string MSN_FOUND_USER = ":%h 642 %n %s %s %s"; //cid + nick + room
    public static string MSN_FOUND_USER_END = ":%h 643 %n :%s";


    public static string MSN_CAT_NOTFOUND = ":%h 701 %n :Category not found";
    public static string MSN_NOTFOUND_CHAN = ":%h 702 %n :%s";
    public static string MSN_DUPLICATE_CHAN = ":%h 705 %n :%s";
    public static string MSN_NOTVALID_CHAN = ":%h 706 %n :%s";
    public static string MSN_INPUTFLOOD = ":%h 708 %n :%s";
    public static string MSN_NOTFOUND_USER = ":%h 709 %n :%s";
}