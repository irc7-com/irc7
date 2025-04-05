using System.Text.RegularExpressions;
using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.IO;
using Irc.Objects.Server;

namespace Irc.Objects.Channel;

public class Channel : ChatObject, IChannel
{
    protected readonly IList<IChannelMember> _members = new List<IChannelMember>();
    public HashSet<string> BanList = new();
    public HashSet<string> InviteList = new();

    public Channel(string name, IChannelModes modes, IDataStore dataStore) : base(modes, dataStore)
    {
        SetName(name);
        DataStore.SetId(Name);
    }

    public override IChannelModes Modes => (IChannelModes)base.Modes;

    // TODO: The ‘l’, ‘b’, ‘k’ mode is stored with the Channel Store.
    public IDataStore ChannelStore => DataStore;

    public string GetName()
    {
        return Name;
    }

    public IChannelMember? GetMember(IUser user)
    {
        foreach (var channelMember in _members)
            if (channelMember.GetUser() == user)
                return channelMember;

        return null;
    }

    public IChannelMember? GetMemberByNickname(string nickname)
    {
        return _members.FirstOrDefault(member =>
            string.Compare(member.GetUser().GetAddress().Nickname, nickname, true) == 0);
    }

    public bool Allows(IUser user)
    {
        if (HasUser(user)) return false;
        return true;
    }

    public virtual IChannel Join(IUser user, EnumChannelAccessResult accessResult = EnumChannelAccessResult.NONE)
    {
        AddMember(user);
        Send(IrcRaws.RPL_JOIN(user, this));
        return this;
    }


    public IChannel SendTopic(IUser user)
    {
        user.Send(Raw.IRCX_RPL_TOPIC_332(user.Server, user, this, DataStore.Get("topic")));
        return this;
    }

    public IChannel SendTopic()
    {
        _members.ToList().ForEach(member => SendTopic(member.GetUser()));
        return this;
    }

    public IChannel SendNames(IUser user)
    {
        Names.ProcessNamesReply(user, this);
        return this;
    }

    public IChannel Part(IUser user)
    {
        Send(IrcRaws.RPL_PART(user, this));
        RemoveMember(user);
        return this;
    }

    public IChannel Quit(IUser user)
    {
        RemoveMember(user);
        return this;
    }

    public IChannel Kick(IUser source, IUser target, string reason)
    {
        Send(Raw.RPL_KICK_IRC(source, this, target, reason));
        RemoveMember(target);
        return this;
    }

    public void SendMessage(IUser user, string message)
    {
        Send(IrcRaws.RPL_PRIVMSG(user, this, message), (ChatObject)user);
    }

    public void SendNotice(IUser user, string message)
    {
        Send(IrcRaws.RPL_NOTICE(user, this, message), (ChatObject)user);
    }

    public IList<IChannelMember> GetMembers()
    {
        return _members;
    }

    public IChannelModes GetModes()
    {
        return (IChannelModes)_modes;
    }

    public bool HasUser(IUser user)
    {
        foreach (var member in _members)
            // TODO: Re-enable below
            //if (CompareUserAddress(user, member.GetUser())) return true;
            if (CompareUserNickname(member.GetUser(), user) || CompareUserAddress(user, member.GetUser()))
                return true;

        return false;
    }

    public bool CanBeModifiedBy(IChatObject source)
    {
        return source is IServer || ((IUser)source).GetChannels().Keys.Contains(this);
    }

    public EnumIrcError CanModifyMember(IChannelMember source, IChannelMember target,
        EnumChannelAccessLevel requiredLevel)
    {
        // Oper check
        if (target.GetUser().GetLevel() >= EnumUserAccessLevel.Guide)
        {
            if (source.GetUser().GetLevel() < EnumUserAccessLevel.Guide) return EnumIrcError.ERR_NOIRCOP;
            // TODO: Maybe there is better raws for below
            if (source.GetUser().GetLevel() < EnumUserAccessLevel.Sysop &&
                source.GetUser().GetLevel() < target.GetUser().GetLevel()) return EnumIrcError.ERR_NOPERMS;
            if (source.GetUser().GetLevel() < EnumUserAccessLevel.Administrator &&
                source.GetUser().GetLevel() < target.GetUser().GetLevel()) return EnumIrcError.ERR_NOPERMS;
        }

        if (source.GetLevel() >= requiredLevel && source.GetLevel() >= target.GetLevel())
            return EnumIrcError.OK;
        if (!source.IsOwner() && (requiredLevel >= EnumChannelAccessLevel.ChatOwner ||
                                  target.GetLevel() >= EnumChannelAccessLevel.ChatOwner))
            return EnumIrcError.ERR_NOCHANOWNER;
        return EnumIrcError.ERR_NOCHANOP;
    }

    public void ProcessChannelError(EnumIrcError error, IServer server, IUser source, ChatObject target,
        string data)
    {
        switch (error)
        {
            case EnumIrcError.ERR_NEEDMOREPARAMS:
            {
                // -> sky-8a15b323126 MODE #test +l hello
                // < - :sky - 8a15b323126 461 Sky MODE +l :Not enough parameters
                source.Send(Raw.IRCX_ERR_NEEDMOREPARAMS_461(server, source, data));
                break;
            }
            case EnumIrcError.ERR_NOCHANOP:
            {
                //:sky-8a15b323126 482 Sky3k #test :You're not channel operator
                source.Send(Raw.IRCX_ERR_CHANOPRIVSNEEDED_482(server, source, this));
                break;
            }
            case EnumIrcError.ERR_NOCHANOWNER:
            {
                //:sky-8a15b323126 482 Sky3k #test :You're not channel operator
                source.Send(Raw.IRCX_ERR_CHANQPRIVSNEEDED_485(server, source, this));
                break;
            }
            case EnumIrcError.ERR_NOIRCOP:
            {
                source.Send(Raw.IRCX_ERR_NOPRIVILEGES_481(server, source));
                break;
            }
            case EnumIrcError.ERR_NOTONCHANNEL:
            {
                source.Send(Raw.IRCX_ERR_NOTONCHANNEL_442(server, source, this));
                break;
            }
            // TODO: The below should not happen
            case EnumIrcError.ERR_NOSUCHNICK:
            {
                source.Send(Raw.IRCX_ERR_NOSUCHNICK_401(server, source, target.Name));
                break;
            }
            case EnumIrcError.ERR_NOSUCHCHANNEL:
            {
                source.Send(Raw.IRCX_ERR_NOSUCHCHANNEL_403(server, source, Name));
                break;
            }
            case EnumIrcError.ERR_CANNOTSETFOROTHER:
            {
                source.Send(Raw.IRCX_ERR_USERSDONTMATCH_502(server, source));
                break;
            }
            case EnumIrcError.ERR_UNKNOWNMODEFLAG:
            {
                source.Send(IrcRaws.IRC_RAW_501(server, source));
                break;
            }
            case EnumIrcError.ERR_NOPERMS:
            {
                source.Send(Raw.IRCX_ERR_SECURITY_908(server, source));
                break;
            }
        }
    }

    public override void Send(string message)
    {
        foreach (var channelMember in _members)
            channelMember.GetUser().Send(message);
    }

    public override void Send(string message, ChatObject u)
    {
        foreach (var channelMember in _members)
            if (channelMember.GetUser() != u)
                channelMember.GetUser().Send(message);
    }

    public override void Send(string message, EnumChannelAccessLevel accessLevel)
    {
        foreach (var channelMember in _members)
            if (channelMember.GetLevel() >= accessLevel)
                channelMember.GetUser().Send(message);
    }

    public virtual EnumChannelAccessResult GetAccess(IUser user, string key, bool IsGoto = false)
    {
        var accessPermissions = GetAccessEx(user, key, IsGoto);
        return accessPermissions == EnumChannelAccessResult.NONE
            ? EnumChannelAccessResult.SUCCESS_GUEST
            : accessPermissions;
    }

    public virtual bool InviteMember(IUser user)
    {
        var address = user.GetAddress().GetAddress();
        return InviteList.Add(address);
    }

    public virtual bool BanMask(Address address)
    {
        var formattedAddress = address.GetAddress();
        return BanList.Add(formattedAddress);
    }

    public virtual bool UnbanMask(Address address)
    {
        var formattedAddress = address.GetAddress();
        return BanList.Remove(formattedAddress);
    }

    protected virtual IChannelMember AddMember(IUser user,
        EnumChannelAccessResult accessResult = EnumChannelAccessResult.NONE)
    {
        var member = new Member.Member(user);

        if (accessResult == EnumChannelAccessResult.SUCCESS_OWNER) member.SetHost(true);
        else if (accessResult == EnumChannelAccessResult.SUCCESS_HOST) member.SetHost(true);
        else if (accessResult == EnumChannelAccessResult.SUCCESS_VOICE) member.SetVoice(true);

        _members.Add(member);
        user.AddChannel(this, member);
        return member;
    }

    private void RemoveMember(IUser user)
    {
        var member = _members.Where(m => m.GetUser() == user).FirstOrDefault();
        if (member != null) _members.Remove(member);
        user.RemoveChannel(this);
    }

    public void SetName(string Name)
    {
        this.Name = Name;
    }

    private static bool CompareUserAddress(IUser user, IUser otherUser)
    {
        if (otherUser == user || otherUser.GetAddress().UserHost == user.GetAddress().UserHost) return true;
        return false;
    }

    private static bool CompareUserNickname(IUser user, IUser otherUser)
    {
        return otherUser.GetAddress().Nickname.ToUpper() == user.GetAddress().Nickname.ToUpper();
    }

    public static bool ValidName(string channel)
    {
        var regex = new Regex(Resources.IrcxChannelRegex);
        return regex.Match(channel).Success;
    }

    public EnumChannelAccessResult GetAccessEx(IUser user, string key, bool IsGoto = false)
    {
        var operCheck = CheckOper(user);
        var keyCheck = CheckMemberKey(user, key);
        var inviteOnlyCheck = CheckInviteOnly(user);
        var userLimitCheck = CheckUserLimit(IsGoto);

        var accessPermissions = (EnumChannelAccessResult)new[]
        {
            (int)operCheck,
            (int)keyCheck,
            (int)inviteOnlyCheck,
            (int)userLimitCheck
        }.Max();

        return accessPermissions;
    }

    protected EnumChannelAccessResult CheckOper(IUser user)
    {
        if (user.GetLevel() >= EnumUserAccessLevel.Guide) return EnumChannelAccessResult.SUCCESS_OWNER;
        return EnumChannelAccessResult.NONE;
    }

    protected EnumChannelAccessResult CheckMemberKey(IUser user, string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return EnumChannelAccessResult.NONE;

        if (Modes.GetModeChar(Resources.ChannelModeKey) == 1)
        {
            if (Modes.Key == key)
                return EnumChannelAccessResult.SUCCESS_MEMBER;
            return EnumChannelAccessResult.ERR_BADCHANNELKEY;
        }

        return EnumChannelAccessResult.NONE;
    }

    protected EnumChannelAccessResult CheckInviteOnly(IUser user)
    {
        if (Modes.InviteOnly)
            return InviteList.Contains(user.GetAddress().GetAddress())
                ? EnumChannelAccessResult.SUCCESS_MEMBER
                : EnumChannelAccessResult.ERR_INVITEONLYCHAN;

        return EnumChannelAccessResult.NONE;
    }

    protected EnumChannelAccessResult CheckUserLimit(bool IsGoto)
    {
        var userLimit = Modes.UserLimit > 0 ? Modes.UserLimit : int.MaxValue;

        if (IsGoto) userLimit = (int)Math.Ceiling(userLimit * 1.2);

        if (GetMembers().Count >= userLimit) return EnumChannelAccessResult.ERR_CHANNELISFULL;
        return EnumChannelAccessResult.NONE;
    }
}