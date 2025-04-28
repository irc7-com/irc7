using System.Collections.Concurrent;
using System.Text;
using Irc.Access.User;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;
using Irc.Objects.Collections;
using Irc.Security.Packages;
using NLog;

namespace Irc.Objects.User;

public class User : ChatObject, IUser
{
    public static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IConnection _connection;
    private readonly IDataRegulator _dataRegulator;
    private readonly IFloodProtectionProfile _floodProtectionProfile;
    private readonly Queue<ModeOperation> _modeOperations = new();
    private bool _authenticated;
    public override IUserProps Props => (IUserProps)base.Props;
    public override IUserModes Modes => (IUserModes)base.Modes;
    public override IAccessList Access => base.Access;

    private long _commandSequence;
    private bool _guest;
    private EnumUserAccessLevel _level;
    private IProtocol _protocol;
    private bool _registered;
    private ISupportPackage _supportPackage;
    public IDictionary<IChannel, IChannelMember> Channels;

    public DateTime LastPing = DateTime.UtcNow;
    public long PingCount;
    public string Client { get; set; } = string.Empty;
    public string Pass { get; set; } = string.Empty;

    public User(
        IConnection connection, 
        IProtocol protocol, 
        IDataRegulator dataRegulator,
        IFloodProtectionProfile floodProtectionProfile,
        IServer server)
    {
        Server = server;
        _connection = connection;
        _protocol = protocol;
        _dataRegulator = dataRegulator;
        _floodProtectionProfile = floodProtectionProfile;
        _supportPackage = new ANON();
        Channels = new ConcurrentDictionary<IChannel, IChannelMember>();
        base.Modes = new UserModes();
        base.Props = new UserProps();
        base.Access = new UserAccess();

        _connection.OnReceive += (sender, s) =>
        {
            LastPing = DateTime.UtcNow;
            PingCount = 0;
            var message = new ChatMessage(_protocol, s);
            if (message.HasCommand) _dataRegulator.PushIncoming(message);
        };

        UserAddress.SetIp(connection.GetIp());
    }

    private UserProfile UserProfile { get; } = new();

    public override EnumUserAccessLevel Level => GetLevel();

    public UserAddress UserAddress { get; set; } = new();

    public bool Utf8 { get; set; }
    public DateTime LastIdle { get; set; } = DateTime.UtcNow;
    public DateTime LoggedOn { get; private set; } = DateTime.UtcNow;

    public IServer Server { get; }

    public void BroadcastToChannels(string data, bool ExcludeUser)
    {
        foreach (var channel in Channels.Keys) channel.Send(data, this);
    }

    public void AddChannel(IChannel channel, IChannelMember member)
    {
        Channels.Add(channel, member);
    }

    public void RemoveChannel(IChannel channel)
    {
        Channels.Remove(channel);
    }

    public KeyValuePair<IChannel, IChannelMember> GetChannelMemberInfo(IChannel channel)
    {
        return Channels.FirstOrDefault(c => c.Key == channel);
    }

    public KeyValuePair<IChannel, IChannelMember> GetChannelInfo(string Name)
    {
        return Channels.FirstOrDefault(c => c.Key.GetName() == Name);
    }

    public IDictionary<IChannel, IChannelMember> GetChannels()
    {
        return Channels;
    }

    public override void Send(string message)
    {
        _dataRegulator.PushOutgoing(message);
    }

    public override void Send(string message, EnumChannelAccessLevel accessLevel)
    {
        Send(message);
    }

    public void Flush()
    {
        var totalBytes = _dataRegulator.GetOutgoingBytes();

        if (_dataRegulator.GetOutgoingBytes() > 0)
        {
            // Compensate for \r\n
            var queueLength = _dataRegulator.GetOutgoingQueueLength();
            var adjustedTotalBytes = totalBytes + queueLength * 2;

            var stringBuilder = new StringBuilder(adjustedTotalBytes);
            for (var i = 0; i < queueLength; i++)
            {
                stringBuilder.Append(_dataRegulator.PopOutgoing());
                stringBuilder.Append("\r\n");
            }

            Log.Info($"Sending[{_protocol.GetType().Name}/{Name}]: {stringBuilder}");
            _connection?.Send(stringBuilder.ToString());
        }
    }

    public void Disconnect(string message)
    {
        // Clean modes
        _modeOperations.Clear();

        Log.Info($"Disconnecting[{_protocol.GetType().Name}/{Name}]: {message}");
        _connection?.Disconnect($"{message}\r\n");
    }

    public IDataRegulator GetDataRegulator()
    {
        return _dataRegulator;
    }

    public IFloodProtectionProfile GetFloodProtectionProfile()
    {
        return _floodProtectionProfile;
    }

    public ISupportPackage GetSupportPackage()
    {
        return _supportPackage;
    }

    public void SetSupportPackage(ISupportPackage supportPackage)
    {
        _supportPackage = supportPackage;
    }

    public void SetProtocol(IProtocol protocol)
    {
        _protocol = protocol;
    }

    public IProtocol GetProtocol()
    {
        return _protocol;
    }

    public IConnection GetConnection()
    {
        return _connection;
    }

    public EnumUserAccessLevel GetLevel()
    {
        return _level;
    }

    public string Nickname
    {
        get => Name;
        set
        {
            Name = value;
            UserAddress.SetNickname(value);
        }
    }

    public void ChangeNickname(string newNick, bool utf8Prefix)
    {
        var nickname = utf8Prefix ? $"'{newNick}" : newNick;
        var rawNicknameChange = Raws.RPL_NICK(Server, this, nickname);
        Send(rawNicknameChange);
        Nickname = nickname;

        foreach (var channel in Channels) channel.Key.Send(rawNicknameChange, this);
    }

    public bool Away { get; set; }

    public UserAddress GetAddress()
    {
        return UserAddress;
    }

    public bool IsGuest()
    {
        return _guest;
    }

    public virtual void SetGuest(bool guest)
    {
        if (Server.DisableGuestMode) return;
        UserProfile.Guest = guest;
        _guest = guest;
    }

    public void SetLevel(EnumUserAccessLevel level)
    {
        _level = level;
    }

    public bool IsRegistered()
    {
        return _registered;
    }

    public bool IsAuthenticated()
    {
        return _authenticated;
    }

    public bool IsOn(IChannel channel)
    {
        return Channels.ContainsKey(channel);
    }

    public bool IsAnon()
    {
        return _supportPackage is ANON;
    }

    public bool IsSysop()
    {
        return Modes.GetModeValue(Resources.UserModeOper) == 1;
    }

    public bool IsAdministrator()
    {
        return Modes.HasMode('a') && Modes.GetModeValue(Resources.UserModeAdmin) == 1;
    }

    public virtual void SetAway(IServer server, IUser user, string message)
    {
        user.Away = true;
        UserProfile.Away = true;
        foreach (var channelPair in user.GetChannels())
        {
            var channel = channelPair.Key;
            channel.Send(Raws.IRCX_RPL_USERNOWAWAY_822(server, user, message), (ChatObject)user);
        }

        user.Send(Raws.IRCX_RPL_NOWAWAY_306(server, user));
    }

    public virtual void SetBack(IServer server, IUser user)
    {
        user.Away = false;
        UserProfile.Away = false;
        foreach (var channelPair in user.GetChannels())
        {
            var channel = channelPair.Key;
            channel.Send(Raws.IRCX_RPL_USERUNAWAY_821(server, user), (ChatObject)user);
        }

        user.Send(Raws.IRCX_RPL_UNAWAY_305(server, user));
    }

    public virtual void PromoteToAdministrator()
    {
        Modes.Admin.ModeValue = true;
        Modes.Admin.DispatchModeChange(this, this, true, string.Empty);
        _level = EnumUserAccessLevel.Administrator;
        UserProfile.Level = EnumUserAccessLevel.Administrator;
        Send(Raws.IRCX_RPL_YOUREADMIN_386(Server, this));
    }

    public virtual void PromoteToSysop()
    {
        Modes.Oper.ModeValue = true;
        Modes.Oper.DispatchModeChange(this, this, true, string.Empty);
        _level = EnumUserAccessLevel.Sysop;
        UserProfile.Level = EnumUserAccessLevel.Sysop;
        Send(Raws.IRCX_RPL_YOUREOPER_381(Server, this));
    }

    public virtual void PromoteToGuide()
    {
        Modes.Oper.ModeValue = true;
        Modes.Oper.DispatchModeChange(this, this, true, string.Empty);
        _level = EnumUserAccessLevel.Guide;
        UserProfile.Level = EnumUserAccessLevel.Guide;
        Send(Raws.IRCX_RPL_YOUREGUIDE_629(Server, this));
    }

    public bool DisconnectIfOutgoingThresholdExceeded()
    {
        if (GetDataRegulator().IsOutgoingThresholdExceeded())
        {
            GetDataRegulator().Purge();
            Disconnect("Output quota exceeded");
            return true;
        }

        return false;
    }

    public bool DisconnectIfIncomingThresholdExceeded()
    {
        // Disconnect user if incoming quota exceeded
        if (GetDataRegulator().IsIncomingThresholdExceeded())
        {
            GetDataRegulator().Purge();
            Disconnect("Input quota exceeded");
            return true;
        }

        return false;
    }

    public void DisconnectIfInactive()
    {
        var seconds = (DateTime.UtcNow.Ticks - LastPing.Ticks) / TimeSpan.TicksPerSecond;
        if (seconds > (PingCount + 1) * Server.PingInterval)
        {
            if (PingCount < Server.PingAttempts)
            {
                Log.Debug($"Ping Count for {this} hit stage {PingCount + 1}");
                PingCount++;
                Send(Raws.RPL_PING(Server, this));
            }
            else
            {
                GetDataRegulator().Purge();
                Disconnect(Raws.IRCX_CLOSINGLINK_011_PINGTIMEOUT(Server, this, _connection.GetIp()));
            }
        }
    }

    public void Register()
    {
        var userAddress = GetAddress();
        var credentials = GetSupportPackage().GetCredentials();

        if (credentials == null) throw new Exception("Register: No credentials provided");

        var userHost = string.IsNullOrWhiteSpace(userAddress.User) ? userAddress.MaskedIp : userAddress.User;
        if (GetSupportPackage() is not ANON)
        {
            var credUser = credentials.GetUsername();
            userHost = string.IsNullOrWhiteSpace(credUser) ? userAddress.MaskedIp : credUser;
        }
        userAddress.User = userHost;
        userAddress.Host = credentials.GetDomain();
        userAddress.Server = Server.Name;

        LoggedOn = DateTime.UtcNow;
        _authenticated = true;
        _registered = true;
    }

    public void Authenticate()
    {
        _authenticated = true;
    }

    public Queue<ModeOperation> GetModeOperations()
    {
        return _modeOperations;
    }

    public IChatFrame GetNextFrame()
    {
        _commandSequence++;
        var message = _dataRegulator.PopIncoming();
        return new ChatFrame
        {
            SequenceId = _commandSequence,
            Server = Server,
            User = this,
            ChatMessage = message
        };
    }

    public UserProfile GetProfile()
    {
        return UserProfile;
    }

    public override bool CanBeModifiedBy(IChatObject source)
    {
        return source == this;
    }
}