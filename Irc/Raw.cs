using System.Globalization;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc;

public static class Raw
{
    public static string IRCX_CLOSINGLINK(IServer server, IUser user, string code, string message)
    {
        return $"ERROR :Closing Link: {user}[{user.GetAddress().RemoteIp}] {code} ({message})";
    }

    public static string IRCX_CLOSINGLINK_007_SYSTEMKILL(IServer server, IUser user, string ip)
    {
        return $"ERROR :Closing Link: {user}[{ip}] 007 (Killed by system operator)";
    }

    public static string IRCX_CLOSINGLINK_008_INPUTFLOODING(IServer server, IUser user)
    {
        return $"ERROR :Closing Link: {user}[%s] 008 (Input flooding)";
    }

    public static string IRCX_CLOSINGLINK_009_OUTPUTSATURATION(IServer server, IUser user)
    {
        return $"ERROR :Closing Link: {user}[%s] 009 (Output Saturation)";
    }

    public static string IRCX_CLOSINGLINK_011_PINGTIMEOUT(IServer server, IUser user, string ip)
    {
        return $"ERROR :Closing Link: {user}[{ip}] 011 (Ping timeout)";
    }

    public static string RPL_AUTH_INIT(IServer server, IUser user)
    {
        return "AUTH %s I :%s";
    }

    public static string RPL_AUTH_SEC_REPLY(string package, string token)
    {
        return $"AUTH {package} S :{token}";
    }

    public static string RPL_AUTH_SUCCESS(string package, string address, int oid)
    {
        return $"AUTH {package} * {address} {oid}";
    }

    public static string RPL_JOIN_IRC(IUser user, IChannel channel)
    {
        return $":{user.GetAddress()} JOIN :{channel}";
    }

    public static string RPL_JOIN_MSN(IServer server, IUser user, IChannel channel)
    {
        return $":{user.GetAddress()} JOIN %S :{channel}";
    }

    public static string RPL_PART_IRC(IUser user, IChannel channel)
    {
        return $":{user.GetAddress()} PART {channel}";
    }

    public static string RPL_QUIT_IRC(IServer server, IUser user)
    {
        return $":{user.GetAddress()} QUIT :%s";
    }

    public static string RPL_MODE_IRC(IUser user, IChatObject target, string modeString)
    {
        return $":{user.GetAddress()} MODE {target} {modeString}";
    }

    public static string RPL_TOPIC_IRC(IServer server, IUser user, IChannel channel, string topic)
    {
        return $":{user.GetAddress()} TOPIC {channel} :{topic}";
    }

    public static string RPL_PROP_IRCX(IServer server, IUser user, ChatObject chatObject, string propName,
        string propValue)
    {
        return $":{user.GetAddress()} PROP {chatObject} {propName} :{propValue}";
    }

    public static string RPL_KICK_IRC(IUser user, IChannel channel, IUser target, string reason)
    {
        return $":{user.GetAddress()} KICK {channel} {target} :{reason}";
    }

    public static string RPL_KILL_IRC(IUser user, IUser targetUser, string message)
    {
        return $":{user.GetAddress()} KILL {targetUser} :{message}";
    }

    public static string RPL_SERVERKILL_IRC(IServer server, IUser user)
    {
        return $":{server} KILL %s :%s";
    }

    public static string RPL_KILLED(IServer server, IUser user)
    {
        return ":%s KILLED";
    }

    public static string RPL_MSG_CHAN(IServer server, IUser user, IChannel channel)
    {
        return $":{user.GetAddress()} %s {channel} :%s";
    }

    public static string RPL_MSG_USER(IServer server, IUser user)
    {
        return $":{user.GetAddress()} %s %s :%s";
    }

    public static string RPL_CHAN_MSG(IServer server, IUser user)
    {
        return ":%s %s %s :%s";
    }

    public static string RPL_CHAN_WHISPER(IServer server, IUser user, IChannel channel, ChatObject target,
        string message)
    {
        return $":{user.GetAddress()} WHISPER {channel} {target} :{message}";
    }

    public static string RPL_EPRIVMSG_CHAN(IServer server, IUser user, IChannel channel)
    {
        return $":{user.GetAddress()} EPRIVMSG {channel} :%s";
    }

    public static string RPL_NOTICE_USER(IServer server, IUser user, IChatObject target, string message)
    {
        return $":{user.GetAddress()} NOTICE {target} :{message}";
    }

    public static string RPL_PRIVMSG_USER(IServer server, IUser user, IChatObject target, string message)
    {
        return $":{user.GetAddress()} PRIVMSG {target} :{message}";
    }

    public static string RPL_NOTICE_CHAN(IServer server, IUser user, IChannel channel)
    {
        return $":{user.GetAddress()} NOTICE {channel} :%s";
    }

    public static string RPL_PRIVMSG_CHAN(IUser user, IChannel channel, string message)
    {
        return $":{user.GetAddress()} PRIVMSG {channel} :{message}";
    }

    public static string RPL_INVITE(IServer server, IUser user, IUser targetUser, string host, IChannel channel)
    {
        return $":{user.GetAddress()} INVITE {targetUser} {host} {channel}";
    }

    public static string RPL_KNOCK_CHAN(IServer server, IUser user, IChannel channel)
    {
        return $":{user.GetAddress()} KNOCK {channel} %s";
    }

    public static string RPL_NICK(IServer server, IUser user, string newNick)
    {
        return $":{user.GetAddress()} NICK {newNick}";
    }

    public static string RPL_PONG(IServer server, IUser user)
    {
        return $"PONG {server} :{user}";
    }

    public static string RPL_PONG_CLIENT(IServer server, IUser user)
    {
        return "PONG :%s";
    }

    public static string RPL_PING(IServer server, IUser user)
    {
        return $"PING :{server}";
    }

    public static string RPL_SERVICE_DATA(IServer server, IUser user)
    {
        return $":{server} SERVICE %s %s";
        //e.g. :IServerNAMEs SERVICE DELETE CHANNEL %#Channel
    }


    public static string IRCX_RPL_WELCOME_001(IServer server, IUser user)
    {
        return $":{server} 001 {user} :Welcome to the {server.Title} server, {user}";
    }

    public static string IRCX_RPL_WELCOME_002(IServer server, IUser user, Version version)
    {
        return
            $":{server} 002 {user} :Your host is {server}, running version {version.Major}.{version.Minor}.{version.Revision}";
    }

    public static string IRCX_RPL_WELCOME_003(IServer server, IUser user)
    {
        return $":{server} 003 {user} :This server was created {server.CreationDate}";
    }

    public static string IRCX_RPL_WELCOME_004(IServer server, IUser user, Version version)
    {
        return
            $":{server} 004 {user} {server} {version.Major}.{version.Minor}.{version.Revision} {server.GetSupportedUserModes()} {server.GetSupportedChannelModes()}";
    }

    public static string IRCX_RPL_UMODEIS_221(IServer server, IUser user, string modes)
    {
        return $":{server} 221 {user} {modes}";
    }

    public static string IRCX_RPL_LUSERCLIENT_251(IServer server, IUser user, int users, int invisible, int servers)
    {
        return $":{server} 251 {user} :There are {users} users and {invisible} invisible on {servers} servers";
    }

    public static string IRCX_RPL_LUSEROP_252(IServer server, IUser user, int operators)
    {
        return $":{server} 252 {user} {operators} :operator(s) online";
    }

    public static string IRCX_RPL_LUSERUNKNOWN_253(IServer server, IUser user, int unknown)
    {
        return $":{server} 253 {user} {unknown} :unknown connection(s)";
    }

    public static string IRCX_RPL_LUSERCHANNELS_254(IServer server, IUser user)
    {
        return $":{server} 254 {user} {server.GetChannels().Count} :channels formed";
    }

    public static string IRCX_RPL_LUSERME_255(IServer server, IUser user, int clients, int servers)
    {
        return $":{server} 255 {user} :I have {clients} clients and {servers} servers";
    }

    public static string IRCX_RPL_ADMINME_256(IServer server, IUser user)
    {
        return $":{server} 256 {user} :Administrative info about {server}";
    }

    public static string IRCX_RPL_ADMINLOC1_257(IServer server, IUser user)
    {
        return $":{server} 257 {user} :%s";
    }

    public static string IRCX_RPL_ADMINLOC1_258(IServer server, IUser user)
    {
        return $":{server} 258 {user} :%s";
    }

    public static string IRCX_RPL_ADMINEMAIL_259(IServer server, IUser user)
    {
        return $":{server} 259 {user} :%s";
    }

    public static string IRCX_RPL_LUSERS_265(IServer server, IUser user, int localUsers, int localMax)
    {
        return $":{server} 265 {user} :Current local users: {localUsers} Max: {localMax}";
    }

    public static string IRCX_RPL_GUSERS_266(IServer server, IUser user, int globalUsers, int globalMax)
    {
        return $":{server} 266 {user} :Current global users: {globalUsers} Max: {globalMax}";
    }

    public static string IRCX_RPL_AWAY_301(IServer server, IUser user, IUser awayUser, string message)
    {
        return $":{server} 301 {user} {awayUser} :{message}";
    }

    public static string IRCX_RPL_USERHOST_302(IServer server, IUser user)
    {
        return $":{server} 302 {user} :{user}={user}!~{user.GetAddress().GetUserHost()}";
    }

    public static string IRCX_RPL_UNAWAY_305(IServer server, IUser user)
    {
        return $":{server} 305 {user} :You are no longer marked as being away";
    }

    public static string IRCX_RPL_NOWAWAY_306(IServer server, IUser user)
    {
        return $":{server} 306 {user} :You have been marked as being away";
    }

    public static string IRCX_RPL_WHOISUSER_311(IServer server, IUser user)
    {
        return $":{server} 311 {user} %s %s %s * :%s";
    }

    public static string IRCX_RPL_WHOISSERVER_312(IServer server, IUser user)
    {
        return $":{server} 312 {user} %s %s :%s";
    }

    public static string IRCX_RPL_WHOISOPERATOR_313A(IServer server, IUser user)
    {
        return $":{server} 313 {user} %s :is an IRC administrator";
    }

    public static string IRCX_RPL_WHOISOPERATOR_313O(IServer server, IUser user)
    {
        return $":{server} 313 {user} %s :is an IRC operator";
    }

    public static string IRCX_RPL_ENDOFWHO_315(IServer server, IUser user, string criteria)
    {
        return $":{server} 315 {user} {criteria} :End of /WHO list";
    }

    public static string IRCX_RPL_WHOISIDLE_317(IServer server, IUser user)
    {
        return $":{server} 317 {user} %s %d %d :seconds idle, signon time";
    }

    public static string IRCX_RPL_ENDOFWHOIS_318(IServer server, IUser user)
    {
        return $":{server} 318 {user} %s :End of /WHOIS list";
    }

    public static string IRCX_RPL_WHOISCHANNELS_319(IServer server, IUser user)
    {
        return $":{server} 319 {user} %s :%s";
    }

    public static string IRCX_RPL_WHOISCHANNELS_319X(IServer server, IUser user)
    {
        return $":{server} 319 {user} %s :";
    }

    public static string IRCX_RPL_WHOISIP_320(IServer server, IUser user, IUser targetUser)
    {
        return $":{server} 320 {user} {targetUser} :from IP {targetUser.GetConnection().GetIp()}";
    }

    public static string IRCX_RPL_MODE_321(IServer server, IUser user)
    {
        return $":{server} 321 {user} Channel :Users  Name";
    }

    public static string IRCX_RPL_MODE_322(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 322 {user} {channel} {channel.GetMembers().Count} :{channel.ChannelStore.Get("topic")}";
    }

    public static string IRCX_RPL_MODE_323(IServer server, IUser user)
    {
        return $":{server} 323 {user} :End of /LIST";
    }

    public static string IRCX_RPL_MODE_324(IServer server, IUser user, IChannel channel, string modes)
    {
        return $":{server} 324 {user} {channel} +{modes}";
    }

    public static string IRCX_RPL_NOTOPIC_331(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 331 {user} {channel} :No topic is set";
    }

    public static string IRCX_RPL_TOPIC_332(IServer server, IUser user, IChannel channel, string topic)
    {
        return $":{server} 332 {user} {channel} :{topic}";
    }

    public static string IRCX_RPL_VERSION_351(IServer server, IUser user, Version version)
    {
        return
            $":{server} 351 {user} {version.Major}.{version.Minor}.{version.Revision}.{version.Build} {server} :{server} {version.Major}.{version.Minor}";
    }

    public static string IRCX_RPL_WHOREPLY_352(IServer server, IUser user, string channelName, string userName,
        string hostName, string serverName, string nickName, string userStatus, int hopCount, string realName)
    {
        return
            $":{server} 352 {user} {channelName} {userName} {hostName} {serverName} {nickName} {userStatus} :{hopCount} {realName}";
    }

    public static string IRCX_RPL_NAMEREPLY_353(IServer server, IUser user, IChannel channel, char channelType,
        string names)
    {
        return $":{server} 353 {user} {channelType} {channel} :{names}";
    }

    public static string IRCX_RPL_NAMEREPLY_353X(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 353 {user} {channel} :";
    }

    public static string IRCX_RPL_ENDOFNAMES_366(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 366 {user} {channel} :End of /NAMES list.";
    }

    public static string IRCX_RPL_BANLIST_367(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 367 {user} {channel} %s";
    }

    public static string IRCX_RPL_ENDOFBANLIST_368(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 368 {user} {channel} :End of Channel Ban List";
    }

    public static string IRCX_RPL_RPL_INFO_371_UPTIME(IServer server, IUser user, DateTime creationDate)
    {
        // TODO: Format creation date
        return $":{server} 371 {user} :On-line since {creationDate}";
    }

    public static string IRCX_RPL_RPL_INFO_371_VERS(IServer server, IUser user, Version version)
    {
        // TODO: Get Full Name
        return $":{server} 371 {user} :{server.Name} {server.ServerVersion.Major}.{server.ServerVersion.Minor}";
    }

    public static string IRCX_RPL_RPL_INFO_371_RUNAS(IServer server, IUser user)
    {
        // TODO: Get Full Name
        return $":{server} 371 {user} :This server is running as an IRC.{server.GetType().Name}";
    }

    public static string IRCX_RPL_RPL_MOTD_372(IServer server, IUser user, string message)
    {
        return $":{server} 372 {user} :- {message}";
    }

    public static string IRCX_RPL_RPL_ENDOFINFO_374(IServer server, IUser user)
    {
        return $":{server} 374 {user} :End of /INFO list";
    }

    public static string IRCX_RPL_RPL_MOTDSTART_375(IServer server, IUser user)
    {
        return $":{server} 375 {user} :- {server} Message of the Day -";
    }

    public static string IRCX_RPL_RPL_ENDOFMOTD_376(IServer server, IUser user)
    {
        return $":{server} 376 {user} :End of /MOTD command";
    }

    public static string IRCX_RPL_YOUREOPER_381(IServer server, IUser user)
    {
        return $":{server} 381 {user} :You are now an IRC operator";
    }

    public static string IRCX_RPL_YOUREADMIN_386(IServer server, IUser user)
    {
        return $":{server} 386 {user} :You are now an IRC administrator";
    }

    public static string IRCX_RPL_TIME_391(IServer server, IUser user)
    {
        //<- :sky-8a15b323126 391 Sky sky-8a15b323126 :Wed Aug 10 18:27:41 2022
        return
            $":{server} 391 {user} {server} :{DateTime.Now.ToString("ddd MMM dd HH:mm:ss yyyy", CultureInfo.CreateSpecificCulture("en-us"))}";
    }

    public static string IRCX_ERR_NOSUCHNICK_401_N(IServer server, IUser user)
    {
        return $":{server} 401 {user} %s :No such nick";
    }

    public static string IRCX_ERR_NOSUCHNICK_401(IServer server, IUser user, string target)
    {
        return $":{server} 401 {user} {target} :No such nick/channel";
    }

    public static string IRCX_ERR_NOSUCHCHANNEL_403(IServer server, IUser user, string channel)
    {
        return $":{server} 403 {user} {channel} :No such channel";
    }

    public static string IRCX_ERR_CANNOTSENDTOCHAN_404(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 404 {user} {channel} :Cannot send to channel";
    }

    public static string IRCX_ERR_TOOMANYCHANNELS_405(IServer server, IUser user, string channel)
    {
        return $":{server} 405 {user} {channel} :You have joined too many channels";
    }

    public static string IRCX_ERR_NOORIGIN_409(IServer server, IUser user)
    {
        return $":{server} 409 {user} :No origin specified";
    }

    public static string IRC_ERR_NORECIPIENT_411(IServer server, IUser user, string command)
    {
        return $":{server} 411 {user} :No recipient given ({command})";
    }

    public static string IRC_ERR_NOTEXT_412(IServer server, IUser user, string command)
    {
        return $":{server} 412 {user} :No text to send ({command})";
    }

    public static string IRCX_ERR_UNKNOWNCOMMAND_421(IServer server, IUser user, string command)
    {
        return $":{server} 421 {user} {command} :Unknown command";
    }

    public static string IRCX_ERR_UNKNOWNCOMMAND_421_T(IServer server, IUser user)
    {
        return $":{server} 421 {user} %s :String parameter must be 160 chars or less";
    }

    public static string IRCX_ERR_NOMOTD_422(IServer server, IUser user)
    {
        return $":{server} 422 {user} :MOTD File is missing";
    }

    public static string IRCX_ERR_ERRONEOUSNICK_432(IServer server, IUser user, string nickname)
    {
        return $":{server} 432 {nickname} :Erroneous nickname";
    }

    public static string IRCX_ERR_NICKINUSE_433(IServer server, IUser user)
    {
        return $":{server} 433 * {user} :Nickname is already in use";
    }

    public static string IRCX_ERR_NONICKCHANGES_439(IServer server, IUser user, string nickname)
    {
        return $":{server} 439 {user} {nickname} :Nick name changes not permitted.";
    }

    public static string IRCX_ERR_NOTONCHANNEL_442(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 442 {user} {channel} :You're not on that channel";
    }

    public static string IRCX_ERR_USERONCHANNEL_443(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 443 {user} {channel} {user} :is already on channel";
    }

    public static string IRCX_ERR_NOTREGISTERED_451(IServer server, IUser user)
    {
        return $":{server} 451 {user} :You have not registered";
    }

    public static string IRCX_ERR_NEEDMOREPARAMS_461(IServer server, IUser user, string command)
    {
        return $":{server} 461 {user} {command} :Not enough parameters";
    }

    public static string IRCX_ERR_ALREADYREGISTERED_462(IServer server, IUser user)
    {
        return $":{server} 462 {user} :You may not reregister";
    }

    public static string IRCX_ERR_KEYSET_467(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 467 {user} {channel} :Channel key already set";
    }

    public static string IRCX_ERR_CHANNELISFULL_471(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 471 {user} {channel} :Cannot join channel (+l)";
    }

    public static string IRCX_ERR_UNKNOWNMODE_472(IServer server, IUser user, char mode)
    {
        return $":{server} 472 {user} {mode} :is unknown mode char to me";
    }

    public static string IRCX_ERR_INVITEONLYCHAN_473(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 473 {user} {channel} :Cannot join channel (+i)";
    }

    public static string IRCX_ERR_BANNEDFROMCHAN_474(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 474 {user} {channel} :Cannot join channel (+b)";
    }

    public static string IRCX_ERR_BADCHANNELKEY_475(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 475 {user} {channel} :Cannot join channel (+k)";
    }

    public static string IRCX_ERR_NOPRIVILEGES_481(IServer server, IUser user)
    {
        return $":{server} 481 {user} :Permission Denied - You're not an IRC operator";
    }

    public static string IRCX_ERR_CHANOPRIVSNEEDED_482(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 482 {user} {channel} :You're not channel operator";
    }

    public static string IRCX_ERR_CHANQPRIVSNEEDED_485(IServer server, IUser user, ChatObject channel)
    {
        return $":{server} 485 {user} {channel} :You're not channel owner";
    }

    public static string IRCX_ERR_USERSDONTMATCH_502(IServer server, IUser user)
    {
        return $":{server} 502 {user} :Cant change mode for other users";
    }

    public static string IRCX_ERR_COMMANDUNSUPPORTED_554(IServer server, IUser user, string command)
    {
        return $":{server} 555 {user} {command} :Command not supported.";
    }

    public static string IRCX_ERR_OPTIONUNSUPPORTED_555(IServer server, IUser user)
    {
        return $":{server} 555 {user} %s :server option for this command is not supported.";
    }

    public static string IRCX_ERR_AUTHONLYCHAN_556(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 556 {user} {channel} :Only authenticated users may join channel.";
    }

    public static string IRCX_ERR_SECUREONLYCHAN_557(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 557 {user} {channel} :Only secure users may join channel.";
    }

    public static string IRCX_RPL_FINDS_613(IServer server, IUser user)
    {
        //return $":{server} 613 {user} :%s %s";
        return $":{server} 613 {user} :{server.RemoteIp} 6667";
    }

    public static string IRCX_RPL_LISTRSTART_614(IServer server, IUser user)
    {
        return $":{server} 811 {user} :Start of ListR";
    }

    public static string IRCX_RPL_LISTRLIST_614(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 812 {user} {channel} %d %s :%s";
    }

    public static string IRCX_RPL_LISTREND_614(IServer server, IUser user)
    {
        return $":{server} 817 {user} :End of ListR";
    }

    public static string IRCX_RPL_YOUREGUIDE_629(IServer server, IUser user)
    {
        return $":{server} 629 {user} :You are now an IRC guide";
    }

    public static string IRC2_RPL_WHOISSECURE_671(IServer server, IUser user, IUser targetUser)
    {
        return $":{server} 671 {user} {targetUser} :is using a secure connection";
    }

    public static string IRCX_RPL_FINDS_NOSUCHCAT_701(IServer server, IUser user)
    {
        return $":{server} 701 {user} :Category not found";
    }

    public static string IRCX_RPL_FINDS_NOTFOUND_702(IServer server, IUser user)
    {
        return $":{server} 702 {user} :Channel not found";
    }

    public static string IRCX_RPL_FINDS_DOWN_703(IServer server, IUser user)
    {
        return $":{server} 703 {user} :Server down. Retry later.";
    }

    public static string IRCX_RPL_FINDS_CHANNELEXISTS_705(IServer server, IUser user)
    {
        return $":{server} 705 {user} :Channel with same name exists";
    }

    public static string IRCX_RPL_FINDS_INVALIDCHANNEL_706(IServer server, IUser user)
    {
        return $":{server} 706 {user} :Channel name is not valid";
    }

    public static string IRCX_RPL_IRCX_800(IServer server, IUser user, int isircx, int ircxversion, int buffsize,
        string options)
    {
        return $":{server} 800 {user} {isircx} {ircxversion} {server.SecurityPackages} {buffsize} {options}";
    }

    public static string IRCX_RPL_ACCESSADD_801(IServer server, IUser user, IChatObject targetObject,
        string accessLevel, string mask, int duration, string address, string reason)
    {
        return $":{server} 801 {user} {targetObject} {accessLevel} {mask} {duration} {address} :{reason}";
    }

    public static string IRCX_RPL_ACCESSDELETE_802(IServer server, IUser user, IChatObject targetObject,
        string accessLevel, string mask, int duration, string address, string reason)
    {
        return $":{server} 802 {user} {targetObject} {accessLevel} {mask} {duration} {address} :{reason}";
    }

    public static string IRCX_RPL_ACCESSSTART_803(IServer server, IUser user, IChatObject targetObject)
    {
        return $":{server} 803 {user} {targetObject} :Start of access entries";
    }

    public static string IRCX_RPL_ACCESSLIST_804(IServer server, IUser user, IChatObject targetObject,
        string accessLevel, string mask, int duration, string address, string reason)
    {
        return $":{server} 804 {user} {targetObject} {accessLevel} {mask} {duration} {address} :{reason}";
    }

    public static string IRCX_RPL_ACCESSEND_805(IServer server, IUser user, IChatObject targetObject)
    {
        return $":{server} 805 {user} {targetObject} :End of access entries";
    }

    public static string IRCX_RPL_EVENTADD_806(IServer server, IUser user)
    {
        return $":{server} 806 {user} %s %s";
    }

    public static string IRCX_RPL_EVENTDEL_807(IServer server, IUser user)
    {
        return $":{server} 807 {user} %s %s";
    }

    public static string IRCX_RPL_EVENTSTART_808(IServer server, IUser user)
    {
        return $":{server} 808 {user} %s :Start of events";
    }

    public static string IRCX_RPL_EVENTLIST_809(IServer server, IUser user)
    {
        return $":{server} 809 {user} %s %s";
    }

    public static string IRCX_RPL_EVENTEND_810(IServer server, IUser user)
    {
        return $":{server} 810 {user} %s :End of events";
    }

    public static string IRCX_RPL_LISTXSTART_811(IServer server, IUser user)
    {
        return $":{server} 811 {user} :Start of ListX";
    }

    public static string IRCX_RPL_LISTXLIST_812(IServer server, IUser user, IChannel channel, string modes,
        int memberCount, int memberLimit, string topic)
    {
        return $":{server} 812 {user} {channel} {modes} {memberCount} {memberLimit} :{topic}";
    }

    public static string IRCX_RPL_LISTXPICS_813(IServer server, IUser user)
    {
        return $":{server} 813 {user} :%s";
    }

    public static string IRCX_RPL_LISTXTRUNC_816(IServer server, IUser user)
    {
        return $":{server} 816 {user} :Truncation of ListX";
    }

    public static string IRCX_RPL_LISTXEND_817(IServer server, IUser user)
    {
        return $":{server} 817 {user} :End of ListX";
    }


    public static string IRCX_RPL_ACCESSCLEAR_820(IServer server, IUser user, IChatObject targetObject,
        EnumAccessLevel accessLevel)
    {
        var level = accessLevel == EnumAccessLevel.All ? "*" : accessLevel.ToString();
        return $":{server} 820 {user} {targetObject} {level} :Clear";
    }

    public static string IRCX_RPL_USERUNAWAY_821(IServer server, IUser user)
    {
        return $":{user.GetAddress()} 821 :User unaway";
    }

    public static string IRCX_RPL_USERNOWAWAY_822(IServer server, IUser user, string reason)
    {
        return $":{user.GetAddress()} 822 :{reason}";
    }

    public static string IRCX_RPL_REVEAL_851(IServer server, IUser user)
    {
        return $":{server} 851 {user} %s %s %s %s :%s";
    }

    public static string IRCX_RPL_REVEALEND_852(IServer server, IUser user)
    {
        return $":{server} 852 {user} :End of ListX";
    }

    public static string IRCX_ERR_BADCOMMAND_900(IServer server, IUser user, string command)
    {
        return $":{server} 900 {user} {command} :Bad command";
    }

    public static string IRCX_ERR_TOOMANYARGUMENTS_901(IServer server, IUser user, string commandName)
    {
        return $":{server} 901 {user} {commandName} :Too many arguments";
    }

    public static string IRCX_ERR_BADLYFORMEDPARAMS_902(IServer server, IUser user)
    {
        return $":{server} 902 {user} :Badly formed parameters";
    }

    public static string IRCX_ERR_BADLEVEL_903(IServer server, IUser user, string level)
    {
        return $":{server} 903 {user} %s :Bad level";
    }

    public static string IRCX_ERR_BADPROPERTY_905(IServer server, IUser user, string property)
    {
        return $":{server} 905 {user} {property} :Bad property specified";
    }

    public static string IRCX_ERR_BADVALUE_906(IServer server, IUser user, string value)
    {
        return $":{server} 906 {user} {value} :Bad value specified";
    }

    public static string IRCX_ERR_RESOURCE_907(IServer server, IUser user)
    {
        return $":{server} 907 {user} :Not enough resources";
    }

    public static string IRCX_ERR_SECURITY_908(IServer server, IUser user)
    {
        return $":{server} 908 {user} :No permissions to perform command";
    }

    public static string IRCX_ERR_ALREADYAUTHENTICATED_909(IServer server, IUser user)
    {
        return $":{server} 909 {user} %s :Already authenticated";
    }

    public static string IRCX_ERR_AUTHENTICATIONFAILED_910(IServer server, IUser user, string package)
    {
        return $":{server} 910 {user} {package} :Authentication failed";
    }

    public static string IRCX_ERR_AUTHENTICATIONSUSPENDED_911(IServer server, IUser user)
    {
        return $":{server} 911 {user} %s :Authentication suspended for this IP";
    }

    public static string IRCX_ERR_UNKNOWNPACKAGE_912(IServer server, IUser user, string package)
    {
        return $":{server} 912 {user} {package} :Unsupported authentication package";
    }

    public static string IRCX_ERR_NOACCESS_913(IServer server, IUser user, IChatObject chatObject)
    {
        return $":{server} 913 {user} {chatObject} :No access";
    }

    public static string IRCX_ERR_DUPACCESS_914(IServer server, IUser user)
    {
        return $":{server} 914 {user} :Duplicate access entry";
    }

    public static string IRCX_ERR_MISACCESS_915(IServer server, IUser user)
    {
        return $":{server} 915 {user} :Unknown access entry";
    }

    public static string IRCX_ERR_TOOMANYACCESSES_916(IServer server, IUser user)
    {
        return $":{server} 916 {user} :Too many access entries";
    }

    public static string IRCX_ERR_EVENTDUP_918(IServer server, IUser user)
    {
        return $":{server} 918 {user} %s %s :Duplicate event entry";
    }

    public static string IRCX_ERR_EVENTMIS_919(IServer server, IUser user)
    {
        return $":{server} 919 {user} %s %s :Unknown event entry";
    }

    public static string IRCX_ERR_NOSUCHEVENT_920(IServer server, IUser user)
    {
        return $":{server} 920 {user} %s :No such event";
    }

    public static string IRCX_ERR_TOOMANYEVENTS_921(IServer server, IUser user)
    {
        return $":{server} 921 {user} %s :Too many events specified";
    }

    public static string IRCX_ERR_ACCESSNOTCLEAR_922(IServer server, IUser user)
    {
        return $":{server} 922 {user} :Some entries not cleared due to security";
    }

    public static string IRCX_ERR_NOWHISPER_923(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 923 {user} {channel} :Does not permit whispers";
    }

    public static string IRCX_ERR_NOSUCHOBJECT_924(IServer server, IUser user, string objectName)
    {
        return $":{server} 924 {user} {objectName} :No such object found";
    }

    public static string IRCX_ERR_ALREADYONCHANNEL_927(IServer server, IUser user, IChannel channel)
    {
        return $":{server} 927 {user} {channel} :Already in the channel.";
    }

    public static string IRCX_ERR_U_NOTINCHANNEL_928(IServer server, IUser user)
    {
        return $"{server} 928 {user} :You're not in a channel";
    }

    public static string IRCX_ERR_TOOMANYINVITES_929(IServer server, IUser user, IUser targetUser,
        IChannel targetChannel)
    {
        return $":{server} 929 {user} {targetUser} {targetChannel} :Cannot invite. Too many invites.";
    }

    public static string IRCX_ERR_NOTIMPLEMENTED(IServer server, IUser user, string command)
    {
        return $":{server} 999 {user} :%s Sorry, this command is not implemented yet.";
    }

    public static string IRCX_ERR_EXCEPTION(IServer server, IUser user)
    {
        return $":{server} 999 {user} :%s Oops! Looks like you've hit a snag here, please can you kindly report this.";
    }

    public static string IRCX_INFO(IServer server, IUser user, string message)
    {
        return $":{server} 000 {user} :{message}";
    }
}