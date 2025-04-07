namespace Irc;

public static class ExtendedResources
{
    public static char ChannelModeAuthOnly = 'a'; // a - NTLM auth only
    public static char ChannelModeCloneable = 'd';
    public static char ChannelModeClone = 'e';
    public static char ChannelModeProfanity = 'f';
    public static char ChannelModeRegistered = 'r';
    public static char ChannelModeNoWhisper = 'w';
    public static char ChannelModeNoGuestWhisper = 'W';
    public static char ChannelModeService = 'z';
    public static char ChannelModeKnock = 'u';
    public static char ChannelModeAuditorium = 'x';

    public static char MemberModeOwner = 'q';
    public static char MemberModeFlagOwner = '.';

    public static char UserModeAdmin = 'a';
    public static char UserModeIrcx = 'x';
    public static char UserModeGag = 'z';

    #region User Properties

    public static string UserPropOid = "OID";
    public static string UserPropName = "NAME";
    public static string UserPropRole = "ROLE";
    public static string UserPropMsnProfile = "MSNPROFILE"; //
    public static string UserPropMsnRegCookie = "MSNREGCOOKIE";
    public static string UserPropNickname = "NICK";
    public static string UserPropPuid = "PUID";
    public static string UserPropIrcvers = "IRCVERS";
    public static string UserPropClient = "CLIENT";

    #endregion

    #region Channel Properties

    public static string ChannelPropName = "NAME";
    public static string ChannelPropTopic = "TOPIC";
    public static string ChannelPropLag = "LAG";
    public static string ChannelPropLanguage = "LANGUAGE";
    public static string ChannelPropSubject = "SUBJECT";
    public static string ChannelPropMemberkey = "MEMBERKEY";
    public static string ChannelPropOwnerkey = "OWNERKEY";
    public static string ChannelPropHostkey = "HOSTKEY";
    public static string ChannelPropCreation = "CREATION";
    public static string ChannelPropOID = "OID";
    public static string ChannelPropPICS = "PICS";
    public static string ChannelPropOnJoin = "ONJOIN";
    public static string ChannelPropOnPart = "ONPART";
    public static string ChannelPropAccount = "ACCOUNT";
    public static string ChannelPropServicePath = "SERVICEPATH";
    public static string ChannelPropClient = "CLIENT";
    public static string ChannelPropClientGuid = "CLIENTGUID";

    #endregion
}