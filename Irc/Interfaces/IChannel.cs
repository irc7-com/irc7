using Irc.Enumerations;
using Irc.Objects;
using Irc.Objects.Channel;

namespace Irc.Interfaces;

public interface IChannel : IChatObject
{
    new IAccessList Access { get; }
    new IChannelModes Modes { get; }
    new IChannelProps Props { get; }
    string GetName();
    IChannelMember? GetMember(IUser user);
    IChannelMember? GetMemberByNickname(string nickname);
    bool HasUser(IUser user);
    new void Send(string message, ChatObject u);
    new void Send(string message);
    new void Send(string message, EnumChannelAccessLevel accessLevel);
    IChannel Join(IUser user, EnumChannelAccessResult accessResult = EnumChannelAccessResult.NONE);
    IChannel Part(IUser user);
    IChannel Quit(IUser user);
    IChannel Kick(IUser source, IUser target, string reason);
    void SendMessage(IUser user, string message);
    void SendNotice(IUser user, string message);
    IList<IChannelMember> GetMembers();
    new bool CanBeModifiedBy(IChatObject source);
    EnumIrcError CanModifyMember(IChannelMember source, IChannelMember target, EnumChannelAccessLevel requiredLevel);

    void ProcessChannelError(EnumIrcError error, IServer server, IUser source, ChatObject target, string data);

    IChannel SendTopic(IUser user);
    IChannel SendTopic();
    IChannel SendNames(IUser user);
    bool Allows(IUser user);
    EnumChannelAccessResult GetAccess(IUser user, string? key, bool isGoto = false);
    bool InviteMember(IUser user);
    long Creation { get; }
    long TopicChanged { get; set; }
    IChannel UpdateTopic(string topic);
}