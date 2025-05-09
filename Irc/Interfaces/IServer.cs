using Irc.Enumerations;
using Irc.Objects;

namespace Irc.Interfaces;

public interface IServer : IChatObject
{
    string Title { get; }
    DateTime CreationDate { get; }
    bool AnnonymousAllowed { get; }
    int ChannelCount { get; }
    IList<ChatObject> IgnoredUsers { get; }
    IList<string> Info { get; }
    int MaxMessageLength { get; }
    int MaxInputBytes { get; }
    int MaxOutputBytes { get; }
    int PingInterval { get; }
    int PingAttempts { get; }
    int MaxChannels { get; }
    int MaxConnections { get; }
    int MaxAuthenticatedConnections { get; }
    int MaxAnonymousConnections { get; }
    int MaxGuestConnections { get; }
    bool BasicAuthentication { get; }
    bool AnonymousConnections { get; }
    int NetInvisibleCount { get; }
    int NetServerCount { get; }
    int NetUserCount { get; }
    string SecurityPackages { get; }
    int SysopCount { get; }
    int UnknownConnectionCount { get; }
    string RemoteIp { set; get; }
    bool DisableGuestMode { set; get; }
    bool DisableUserRegistration { get; set; }
    bool IsDirectoryServer { get; }
    new Guid Id { get; }
    new string ShortId { get; }
    new string Name { get; set; }
    Version ServerVersion { set; get; }
    void AddUser(IUser user);
    void RemoveUser(IUser user);
    void AddChannel(IChannel channel);
    void RemoveChannel(IChannel channel);
    IChannel CreateChannel(string name);
    IChannel CreateChannel(IUser creator, string name, string key);
    IChannel CreateChannel(
        IUser creator,
        string name,
        string key,
        string locale,
        string category,
        EnumSubjectLevel? subjectLevel = EnumSubjectLevel.User,
        EnumSubjectZone? subjectZone = null
    );
    IUser CreateUser(IConnection connection);
    IList<IUser> GetUsers();
    IUser? GetUserByNickname(string nickname);
    IUser? GetUserByNickname(string nickname, IUser currentUser);
    IList<IUser> GetUsersByList(string nicknames, char separator);
    IList<IUser> GetUsersByList(List<string> nicknames, char separator);
    IList<IChannel> GetChannels();
    string GetSupportedChannelModes();
    string GetSupportedUserModes();
    IDictionary<EnumProtocolType, IProtocol> GetProtocols();
    IDataStore GetDataStore();
    IChannel? GetChannelByName(string name);
    ChatObject? GetChatObject(string name);
    IProtocol? GetProtocol(EnumProtocolType protocolType);
    ISecurityManager GetSecurityManager();
    ICredentialProvider? GetCredentialManager();
    void Shutdown();
    new string ToString();
    string[] GetMotd();
    void SetMotd(string motd);
    void ProcessCookie(IUser user, string name, string value);
}
