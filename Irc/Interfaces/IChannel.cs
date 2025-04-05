using Irc.Enumerations;
using Irc.IO;
using Irc.Objects;
using Irc.Objects.Server;

namespace Irc.Interfaces;

public interface IChannel
{
    IDataStore ChannelStore { get; }
    IChannelModes Modes { get; }
    string GetName();
    IChannelMember? GetMember(IUser User);
    IChannelMember GetMemberByNickname(string nickname);
    bool HasUser(IUser user);
    void Send(string message, ChatObject u);
    void Send(string message);
    void Send(string message, EnumChannelAccessLevel accessLevel);
    IChannel Join(IUser user, EnumChannelAccessResult accessResult = EnumChannelAccessResult.NONE);
    IChannel Part(IUser user);
    IChannel Quit(IUser user);
    IChannel Kick(IUser source, IUser target, string reason);
    void SendMessage(IUser user, string message);
    void SendNotice(IUser user, string message);
    IList<IChannelMember> GetMembers();
    bool CanBeModifiedBy(IChatObject source);
    EnumIrcError CanModifyMember(IChannelMember source, IChannelMember target, EnumChannelAccessLevel requiredLevel);

    void ProcessChannelError(EnumIrcError error, IServer server, IUser source, ChatObject target, string data);

    IChannel SendTopic(IUser user);
    IChannel SendTopic();
    IChannel SendNames(IUser user);
    bool Allows(IUser user);
    IChannelModes GetModes();
    EnumChannelAccessResult GetAccess(IUser user, string key, bool IsGoto = false);
    bool InviteMember(IUser user);
}