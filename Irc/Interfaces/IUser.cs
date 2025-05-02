using Irc.Enumerations;
using Irc.Modes;
using Irc.Objects.User;

namespace Irc.Interfaces;

public interface IUser
{
    IServer Server { get; }
    Guid Id { get; }
    string ShortId { get; }
    string Name { get; set; }
    string Nickname { get; set; }
    string Client { get; set; }
    string Pass { get; set; }
    bool Away { get; set; }
    DateTime LastIdle { get; set; }
    DateTime LoggedOn { get; }
    IModeCollection Modes { get; }
    IUserProps Props { get; }
    bool Utf8 { get; set; }
    IChatFrame? GetNextFrame();
    void ChangeNickname(string newNick, bool utf8Prefix);
    void SetGuest(bool guest);
    void SetAway(IServer server, IUser user, string message);
    void SetBack(IServer server, IUser user);
    void SetLevel(EnumUserAccessLevel level);
    void BroadcastToChannels(string data, bool ExcludeUser);
    void AddChannel(IChannel channel, IChannelMember member);
    void RemoveChannel(IChannel channel);
    KeyValuePair<IChannel, IChannelMember> GetChannelMemberInfo(IChannel channel);
    KeyValuePair<IChannel, IChannelMember> GetChannelInfo(string Name);
    IDictionary<IChannel, IChannelMember> GetChannels();
    void Send(string message);
    void Send(string message, EnumChannelAccessLevel accessLevel);
    void Flush();
    void Disconnect(string message);
    IDataRegulator GetDataRegulator();
    IFloodProtectionProfile GetFloodProtectionProfile();
    ISupportPackage GetSupportPackage();
    void SetSupportPackage(ISupportPackage supportPackage);
    void SetProtocol(IProtocol protocol);
    IProtocol GetProtocol();
    IConnection GetConnection();
    EnumUserAccessLevel GetLevel();
    UserAddress GetAddress();
    bool IsGuest();
    bool IsRegistered();
    bool IsAuthenticated();
    bool IsAnon();
    bool IsSysop();
    bool IsAdministrator();
    bool IsOn(IChannel channel);
    void PromoteToAdministrator();
    void PromoteToSysop();
    void PromoteToGuide();
    bool DisconnectIfOutgoingThresholdExceeded();
    bool DisconnectIfIncomingThresholdExceeded();
    string ToString();
    void Register();
    void Authenticate();
    void DisconnectIfInactive();
    Queue<ModeOperation> GetModeOperations();
}