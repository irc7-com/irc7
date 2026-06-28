using System.Text.RegularExpressions;
using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;
using Irc.Objects.User;

namespace Irc.Objects.Channel;

public class Channel : ChatObject, IChannel
{
    /// <summary>
    /// The authoritative member list. All access (reads and mutations) is guarded by
    /// <see cref="_membersLock"/>. Reads return a defensive copy so callers can iterate
    /// safely without holding the lock, and so concurrent joins observe a consistent
    /// pre-mutation snapshot.
    ///
    /// A plain <see cref="List{T}"/> + lock is used (rather than ImmutableList + CAS)
    /// because adds are O(1) amortized with no per-add allocations or CAS retries. The
    /// server's single-threaded Process() loop means the lock is essentially uncontended
    /// in production, while still providing correctness under the concurrent-join race.
    /// </summary>
    private readonly List<IChannelMember> _members = new();
    private readonly object _membersLock = new();
    public HashSet<string> InviteList = new();
    public string Locale { get; set; } = string.Empty;
    public long Creation { get; } = Resources.GetEpochNowInSeconds();
    public long TopicChanged { get; set; } = Resources.GetEpochNowInSeconds();

    public new IAccessList Access => base.Access;
    public new IChannelProps Props => (IChannelProps)base.Props;
    public new IChannelModes Modes => (IChannelModes)base.Modes;

    public Channel(string name)
    {
        base.Modes = new ChannelModes();
        base.Props = new ChannelProps();
        base.Access = new ChannelAccess();
        
        Name = name;
        Props.Name.Value = name;
        Props.Creation.Value = Creation.ToString();
    }

    private static readonly Lazy<char[]> _cachedSupportedChannelModes = new(() =>
    {
        // Exclude member-specific mode letters (owner/host/voice)
        var excluded = new HashSet<char>
        {
            Resources.MemberModeOwner,
            Resources.MemberModeVoice,
            Resources.MemberModeHost
        };

        var channelModes = new ChannelModes().GetSupportedModes();
        return channelModes.Where(c => !excluded.Contains(c)).ToArray();
    });

    public static char[] SupportedChannelModes()
    {
        // Return a copy to avoid callers mutating the cached array
        return _cachedSupportedChannelModes.Value.ToArray();
    }
    
    public static IChannel FromInMemoryChannel(InMemoryChannel inMemoryChannel)
    {
        var supportedChannelModes = SupportedChannelModes();
        // Set name
        var channel = new Channel(inMemoryChannel.ChannelName);
        
        // Set topic
        channel.UpdateTopic(inMemoryChannel.ChannelTopic);
        
        // Set modes block
        foreach (char c in inMemoryChannel.Modes)
        {
            if (!supportedChannelModes.Contains(c)) continue;
            if (channel.Modes.HasMode(c)) channel.Modes.SetModeValue(c, 1);
        }
        
        // Set user limit
        channel.Modes.SetModeValue(Resources.ChannelModeUserLimit, inMemoryChannel.UserLimit);
        
        // Set category
        channel.Props.Category.Value = inMemoryChannel.Category;
        
        // Set locale
        // 1:+ST!EN-US!AV
        // 1:+ST 1:ST 1:-ST -- No idea
        var isRegistered = inMemoryChannel.Modes.Contains('r');
        var registeredToken = isRegistered ? "+ST" : "-ST";
        var locale = string.IsNullOrWhiteSpace(inMemoryChannel.Locale) ? "EN-US" : inMemoryChannel.Locale;
        var category = string.IsNullOrWhiteSpace(inMemoryChannel.Category) ? "GN" : inMemoryChannel.Category;
        channel.Locale = inMemoryChannel.Locale;
        channel.Props.Subject.Value = $"{inMemoryChannel.Language}:{registeredToken}:{locale}:{category}";
        
        // Set language
        channel.Props.Language.Value = inMemoryChannel.Language.ToString();
        
        // Set ownerkey
        channel.Props.OwnerKey.Value = inMemoryChannel.OwnerKey;
        
        // Set hostkey
        channel.Props.HostKey.Value = inMemoryChannel.HostKey;
        
        return channel;
    }

    public bool Store { get; set; } = false;
    public DateTime? EmptySince { get; set; } = null;

    public string GetName()
    {
        return Name;
    }

    public IChannelMember? GetMember(IUser user)
    {
        foreach (var channelMember in SnapshotMembers())
            if (channelMember.GetUser() == user)
                return channelMember;

        return null;
    }

    public IChannelMember? GetMemberByNickname(string nickname)
    {
        return SnapshotMembers().FirstOrDefault(member =>
            String.Compare(member.GetUser().GetAddress().Nickname, nickname, StringComparison.OrdinalIgnoreCase) == 0);
    }

    public bool Allows(IUser user)
    {
        if (HasUser(user)) return false;
        return true;
    }

    public IChannel Join(IUser user, EnumChannelAccessResult accessResult = EnumChannelAccessResult.NONE)
    {
        // Snapshot existing members AND add this user inside a single lock, so the two
        // steps are atomic with respect to other joins/parts. The snapshot is taken
        // BEFORE the new member is added, guaranteeing that:
        //   • Only users already in the channel receive the JOIN broadcast.
        //   • Two users joining concurrently never appear in each other's broadcast
        //     snapshot — they discover each other exclusively via the 353 NAMREPLY.
        // The ToList() copy lets us broadcast outside the lock, keeping the critical
        // section to O(n) copy + O(1) add and avoiding re-entrant Send() under the lock.
        IList<IChannelMember> existingMembers;
        IChannelMember joinMember;
        lock (_membersLock)
        {
            existingMembers = _members.ToList();
            joinMember = AddMember(user, accessResult);
        }

        // Notify every user who was already in the channel about the new joiner.
        foreach (var channelMember in existingMembers)
        {
            var channelUser = channelMember.GetUser();
            var channelUserProtocol = channelUser.GetProtocol().GetProtocolType();
            if (channelUserProtocol <= EnumProtocolType.IRC3)
            {
                channelUser.Send(Raws.RPL_JOIN(user, this));
            }
            else
            {
                channelUser.Send(Raws.RPL_JOIN_MSN(channelMember, this, joinMember));
            }

            if (channelUserProtocol <= EnumProtocolType.IRC6 && joinMember.HasModes())
            {
                var modeChar = joinMember.Owner.ModeValue ?
                    Resources.MemberModeOwner :
                    joinMember.Operator.ModeValue ?
                        Resources.MemberModeHost : Resources.MemberModeVoice;

                ModeRule.DispatchModeChange((ChatObject)channelUser, modeChar,
                    (ChatObject)user, this, true, user.ToString());
            }
        }

        // Send the new user their own JOIN confirmation.
        var joinUserProtocol = user.GetProtocol().GetProtocolType();
        if (joinUserProtocol <= EnumProtocolType.IRC3)
        {
            user.Send(Raws.RPL_JOIN(user, this));
        }
        else
        {
            user.Send(Raws.RPL_JOIN_MSN(joinMember, this, joinMember));
        }

        // Dispatch the new user's own member mode (owner/host/voice) to themselves.
        if (joinUserProtocol <= EnumProtocolType.IRC6 && joinMember.HasModes())
        {
            var modeChar = joinMember.Owner.ModeValue ?
                Resources.MemberModeOwner :
                joinMember.Operator.ModeValue ?
                    Resources.MemberModeHost : Resources.MemberModeVoice;

            ModeRule.DispatchModeChange((ChatObject)user, modeChar,
                (ChatObject)user, this, true, user.ToString());
        }

        return this;
    }

    public IChannel UpdateTopic(string topic)
    {
        Props.Topic.Value = topic;
        TopicChanged = Resources.GetEpochNowInSeconds();
        return this;
    }
    public IChannel SendOnJoinMessage(IUser user)
    {
        if (string.IsNullOrWhiteSpace(Props.Onjoin.Value)) return this;

        foreach (var se in Props.Onjoin.Value.Split(@"\n", StringSplitOptions.RemoveEmptyEntries).ToList())
        {
            user.Send(Raws.RPL_PRIVMSG_CHAN(
                    this,
                    user,
                    se
                ));
        }

        return this;
    }
    
    public IChannel SendOnPartMessage(IUser user)
    {
        if (string.IsNullOrWhiteSpace(Props.Onpart.Value)) return this;
        
        foreach (var se in Props.Onpart.Value.Split(@"\n", StringSplitOptions.RemoveEmptyEntries).ToList())
        {
            user.Send(Raws.RPL_NOTICE_CHAN(
                this,
                user,
                se
            ));
        }
        
        return this;
    }
    
    public IChannel SendTopic(IUser user)
    {
        user.Send(Raws.IRCX_RPL_TOPIC_332(user.Server, user, this, Props.Topic.Value));
        return this;
    }

    public IChannel SendTopic()
    {
        SnapshotMembers().ForEach(member => SendTopic(member.GetUser()));
        return this;
    }

    public IChannel SendNames(IUser user)
    {
        Names.ProcessNamesReply(user, this);
        return this;
    }

    public IChannel Part(IUser user)
    {
        Send(Raws.RPL_PART(user, this));
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
        Send(Raws.RPL_KICK_IRC(source, this, target, reason));
        RemoveMember(target);
        return this;
    }

    public void SendMessage(IUser user, string message)
    {
        Send(Raws.RPL_PRIVMSG(user, this, message), (ChatObject)user);
    }

    public void SendNotice(IUser user, string message)
    {
        Send(Raws.RPL_NOTICE(user, this, message), (ChatObject)user);
    }

    /// <summary>
    /// Returns a point-in-time copy of the member list taken under the lock.
    /// Callers can iterate the result freely without holding the lock and without
    /// risk of "collection modified" exceptions from concurrent joins/parts.
    /// </summary>
    private List<IChannelMember> SnapshotMembers()
    {
        lock (_membersLock)
        {
            return _members.ToList();
        }
    }

    public IList<IChannelMember> GetMembers()
    {
        return SnapshotMembers();
    }

    public bool HasUser(IUser user)
    {
        foreach (var member in SnapshotMembers())
            if (CompareUserNickname(member.GetUser(), user) || CompareUserAddress(user, member.GetUser()))
                return true;

        return false;
    }

    public override bool CanBeModifiedBy(IChatObject source)
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
        if (!source.Owner.ModeValue && (requiredLevel >= EnumChannelAccessLevel.ChatOwner ||
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
                source.Send(Raws.IRCX_ERR_NEEDMOREPARAMS_461(server, source, data));
                break;
            }
            case EnumIrcError.ERR_NOCHANOP:
            {
                //:sky-8a15b323126 482 Sky3k #test :You're not channel operator
                source.Send(Raws.IRCX_ERR_CHANOPRIVSNEEDED_482(server, source, this));
                break;
            }
            case EnumIrcError.ERR_NOCHANOWNER:
            {
                //:sky-8a15b323126 482 Sky3k #test :You're not channel operator
                source.Send(Raws.IRCX_ERR_CHANQPRIVSNEEDED_485(server, source, this));
                break;
            }
            case EnumIrcError.ERR_NOIRCOP:
            {
                source.Send(Raws.IRCX_ERR_NOPRIVILEGES_481(server, source));
                break;
            }
            case EnumIrcError.ERR_NOTONCHANNEL:
            {
                source.Send(Raws.IRCX_ERR_NOTONCHANNEL_442(server, source, this));
                break;
            }
            // TODO: The below should not happen
            case EnumIrcError.ERR_NOSUCHNICK:
            {
                source.Send(Raws.IRCX_ERR_NOSUCHNICK_401(server, source, target.Name));
                break;
            }
            case EnumIrcError.ERR_NOSUCHCHANNEL:
            {
                source.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(server, source, Name));
                break;
            }
            case EnumIrcError.ERR_CANNOTSETFOROTHER:
            {
                source.Send(Raws.IRCX_ERR_USERSDONTMATCH_502(server, source));
                break;
            }
            case EnumIrcError.ERR_UNKNOWNMODEFLAG:
            {
                source.Send(Raws.IRC_RAW_501(server, source));
                break;
            }
            case EnumIrcError.ERR_NOPERMS:
            {
                source.Send(Raws.IRCX_ERR_SECURITY_908(server, source));
                break;
            }
        }
    }

    public override void Send(string message)
    {
        foreach (var channelMember in SnapshotMembers())
            channelMember.GetUser().Send(message);
    }

    public override void Send(string message, ChatObject u)
    {
        foreach (var channelMember in SnapshotMembers())
            if (channelMember.GetUser() != u)
                channelMember.GetUser().Send(message);
    }

    public override void Send(string message, EnumChannelAccessLevel accessLevel)
    {
        foreach (var channelMember in SnapshotMembers())
            if (channelMember.GetLevel() >= accessLevel)
                channelMember.GetUser().Send(message);
    }

    public EnumChannelAccessResult GetAccess(IUser user, string? key, bool isGoto = false)
    {
        var hostKeyCheck = CheckHostKey(user, key);

        var accessLevel = GetChannelAccess(user);
        var accessResult = EnumChannelAccessResult.NONE;

        switch (accessLevel)
        {
            case EnumAccessLevel.OWNER:
            {
                accessResult = EnumChannelAccessResult.SUCCESS_OWNER;
                break;
            }
            case EnumAccessLevel.HOST:
            {
                accessResult = EnumChannelAccessResult.SUCCESS_HOST;
                break;
            }
            case EnumAccessLevel.VOICE:
            {
                accessResult = EnumChannelAccessResult.SUCCESS_VOICE;
                break;
            }
            case EnumAccessLevel.GRANT:
            {
                accessResult = EnumChannelAccessResult.SUCCESS_MEMBER;
                break;
            }
            case EnumAccessLevel.DENY:
            {
                accessResult = EnumChannelAccessResult.ERR_BANNEDFROMCHAN;
                break;
            }
        }

        var accessPermissions = (EnumChannelAccessResult)new[]
        {
            (int)GetAccessEx(user, key, isGoto),
            (int)hostKeyCheck,
            (int)accessResult
        }.Max();

        return accessPermissions == EnumChannelAccessResult.NONE
            ? EnumChannelAccessResult.SUCCESS_GUEST
            : accessPermissions;
    }

    public bool InviteMember(IUser user)
    {
        var address = user.GetAddress().GetAddress();
        return InviteList.Add(address);
    }


    public EnumAccessLevel GetChannelAccess(IUser user)
    {
        var userAccessLevel = EnumAccessLevel.NONE;
        var addr = user.GetAddress();
        var accessEntries = Access.GetEntries();

        foreach (var accessKvp in accessEntries)
        {
            var accessLevel = accessKvp.Key;
            var accessList = accessKvp.Value;

            if (accessList.Any(entry => UserAddress.Matches(addr, entry.Mask)))
            {
                if ((int)accessLevel > (int)userAccessLevel)
                    userAccessLevel = accessLevel;
            }
        }

        return userAccessLevel;
    }

    protected EnumChannelAccessResult CheckHostKey(IUser user, string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return EnumChannelAccessResult.NONE;

        if (Props.GetProp("OWNERKEY")?.GetValue(this) == key)
            return EnumChannelAccessResult.SUCCESS_OWNER;
        if (Props.GetProp("HOSTKEY")?.GetValue(this) == key) return EnumChannelAccessResult.SUCCESS_HOST;
        return EnumChannelAccessResult.NONE;
    }

    protected IChannelMember AddMember(IUser user,
        EnumChannelAccessResult accessResult = EnumChannelAccessResult.NONE)
    {
        var member = new Member.Member(user);

        if (accessResult == EnumChannelAccessResult.SUCCESS_OWNER) member.Owner.ModeValue = true;
        else if (accessResult == EnumChannelAccessResult.SUCCESS_HOST) member.Operator.ModeValue = true;
        else if (accessResult == EnumChannelAccessResult.SUCCESS_VOICE) member.Voice.ModeValue = true;

        // O(1) amortized add. lock is re-entrant, so this is safe whether called
        // standalone or nested inside Join()'s snapshot-and-add critical section.
        lock (_membersLock)
        {
            _members.Add(member);
        }

        user.AddChannel(this, member);
        EmptySince = null; // Channel is no longer empty
        return member;
    }

    private void RemoveMember(IUser user)
    {
        bool isEmpty;
        lock (_membersLock)
        {
            var target = _members.FirstOrDefault(m => m.GetUser() == user);
            if (target != null) _members.Remove(target);
            isEmpty = _members.Count == 0;
        }

        user.RemoveChannel(this);

        if (isEmpty && !Store)
        {
            EmptySince = DateTime.UtcNow;
        }
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

    public EnumChannelAccessResult GetAccessEx(IUser user, string? key, bool IsGoto = false)
    {
        var operCheck = CheckOper(user);
        var keyCheck = CheckMemberKey(user, key);
        var inviteOnlyCheck = CheckInviteOnly(user);
        var userLimitCheck = CheckUserLimit(IsGoto);
        var authOnlyCheck = CheckAuthOnly(user);

        var accessPermissions = (EnumChannelAccessResult)new[]
        {
            (int)operCheck,
            (int)keyCheck,
            (int)inviteOnlyCheck,
            (int)userLimitCheck,
            (int)authOnlyCheck
        }.Max();

        return accessPermissions;
    }

    protected EnumChannelAccessResult CheckOper(IUser user)
    {
        if (user.GetLevel() >= EnumUserAccessLevel.Guide) return EnumChannelAccessResult.SUCCESS_OWNER;
        return EnumChannelAccessResult.NONE;
    }

    protected EnumChannelAccessResult CheckMemberKey(IUser user, string? key)
    {
        if (Modes.Key.ModeValue)
        {
            if (Props.MemberKey.Value == key)
                return EnumChannelAccessResult.SUCCESS_MEMBER;
            return EnumChannelAccessResult.ERR_BADCHANNELKEY;
        }

        return EnumChannelAccessResult.NONE;
    }

    protected EnumChannelAccessResult CheckInviteOnly(IUser user)
    {
        if (Modes.InviteOnly.ModeValue)
            return InviteList.Contains(user.GetAddress().GetAddress())
                ? EnumChannelAccessResult.SUCCESS_MEMBER
                : EnumChannelAccessResult.ERR_INVITEONLYCHAN;

        return EnumChannelAccessResult.NONE;
    }

    protected EnumChannelAccessResult CheckAuthOnly(IUser user)
    {
        if (!Modes.AuthOnly.ModeValue) return EnumChannelAccessResult.NONE;

        var sasl = user.GetSspiHandler();
        var authenticatedViaNtlm = sasl != null
            && sasl.IsAuthenticated()
            && string.Equals(sasl.GetPackageName(), "NTLM", StringComparison.OrdinalIgnoreCase);

        return authenticatedViaNtlm
            ? EnumChannelAccessResult.NONE
            : EnumChannelAccessResult.ERR_AUTHONLYCHAN;
    }

    protected EnumChannelAccessResult CheckUserLimit(bool IsGoto)
    {
        var userLimit = Modes.UserLimit.Value > 0 ? Modes.UserLimit.Value : int.MaxValue;

        if (IsGoto) userLimit = (int)Math.Ceiling(userLimit * 1.2);

        if (GetMembers().Count >= userLimit) return EnumChannelAccessResult.ERR_CHANNELISFULL;
        return EnumChannelAccessResult.NONE;
    }
    
    public static bool IsAllowedCategory(string category) =>
        Resources.SupportedChannelCategories.Contains(category);

    public static bool IsAllowedLocale(string region) =>
        Resources.SupportedChannelCountryLanguages.Contains(region);

    // A value from 1 to 24 as per apollo docs
    public static bool IsAllowedLanguage(int language) => language >= 1 && language <= 24;
    
    public static bool IsSupportedKey(string key) => key.Length <= 31;

    public static bool IsModeSupported(IServer server, string modes)
    {
        if (modes != "-")
        {
            var supportedModes = server.GetSupportedChannelModes().ToCharArray().ToList();
            var inputModes = modes.ToCharArray().ToList();
            foreach (var mode in inputModes)
            {
                if (mode != 'l' && !supportedModes.Contains(mode))
                {
                    return false;
                }
            }
        }

        return true;
    }
}