using System.Collections.Concurrent;
using System.Text;
using Irc.Access;
using Irc.Access.Server;
using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Infrastructure;
using Irc.Interfaces;
using Irc.IO;
using Irc.Modes;
using Irc.Objects.Channel;
using Irc.Objects.Collections;
using Irc.Objects.User;
using Irc.Protocols;
using Irc.Security.Credentials;
using Irc.Security.Packages;
using Irc.Security.Passport;
using NLog;
using Version = System.Version;

namespace Irc.Objects.Server;

public class Server : ChatObject, IServer
{
    public static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ICredentialProvider? _credentialProvider;
    protected readonly IDataStore _DataStore;
    private readonly IFloodProtectionManager _floodProtectionManager;
    private readonly PassportV4 _passport = new(string.Empty, string.Empty);
    private readonly ConcurrentQueue<IUser> _pendingNewUserQueue = new();
    private readonly ConcurrentQueue<IUser> _pendingRemoveUserQueue = new();
    // Track IDs of users pending removal to avoid duplicate enqueues
    private readonly ConcurrentDictionary<Guid, byte> _pendingRemoveUserSet = new();
    private readonly Task _processingTask;
    private readonly ISecurityManager _securityManager;
    private readonly ISocketServer _socketServer;

    public IList<IChannel> Channels;
    public IDictionary<EnumProtocolType, IProtocol> Protocols = new Dictionary<EnumProtocolType, IProtocol>();

    public IList<IUser> Users = new List<IUser>();

    public Server(ISocketServer socketServer,
        ISecurityManager securityManager,
        IFloodProtectionManager floodProtectionManager,
        IDataStore dataStore,
        IList<IChannel> channels,
        ICredentialProvider? credentialProvider = null)
    {
        Name = dataStore.Get("Name");
        Title = Name;
        _socketServer = socketServer;
        _securityManager = securityManager;
        _floodProtectionManager = floodProtectionManager;
        _DataStore = dataStore;
        Channels = channels;
        _processingTask = new Task(Process);
        _processingTask.Start();

        LoadSettingsFromDataStore();

        _DataStore.SetAs("creation", DateTime.UtcNow);
        _DataStore.Set("supported.channel.modes",
            new ChannelModes().GetSupportedModes());
        _DataStore.Set("supported.user.modes", new UserModes().GetSupportedModes());
        SupportPackages = _DataStore.GetAs<List<string>>(Resources.ConfigSaslPackages)?.ToArray() ??
                          Array.Empty<string>();

        if (MaxAnonymousConnections > 0) _securityManager.AddSupportPackage(new ANON());
        
        //IRCX Initialization
        _credentialProvider = credentialProvider;
        Props = new PropCollection();
        Access = new ServerAccess();

        if (SupportPackages.Contains("NTLM"))
            GetSecurityManager()
                .AddSupportPackage(new NTLM(credentialProvider ?? new NtlmProvider()));

        AddProtocol(EnumProtocolType.IRC, new Protocols.Irc());
        AddProtocol(EnumProtocolType.IRCX, new IrcX());
        AddProtocol(EnumProtocolType.IRC3, new Irc3());
        AddProtocol(EnumProtocolType.IRC4, new Irc4());
        AddProtocol(EnumProtocolType.IRC5, new Irc5());
        AddProtocol(EnumProtocolType.IRC6, new Irc6());
        AddProtocol(EnumProtocolType.IRC7, new Irc7());
        AddProtocol(EnumProtocolType.IRC8, new Irc8());
        
        socketServer.OnClientConnecting += (sender, connection) =>
        {
            // TODO: Need to pass a Interfaced factory in to create the appropriate user
            // TODO: Need to start a new user out with protocol, below code is unreliable
            var user = CreateUser(connection);
            AddUser(user);

            connection.OnConnect += (o, integer) => { Log.Info("Connect"); };
            connection.OnReceive += (o, s) =>
            {
                //Console.WriteLine("OnRecv:" + s);
            };
            connection.OnDisconnect += (o, integer) => RemoveUser(user);
            connection.Accept();
        };
        socketServer.Listen();

        if (SupportPackages.Contains("GateKeeper"))
        {
            _passport = new PassportV4(dataStore.Get("Passport.V4.AppID"), dataStore.Get("Passport.V4.Secret"));
            securityManager.AddSupportPackage(new GateKeeper(new DefaultProvider()));
            securityManager.AddSupportPackage(new GateKeeperPassport(new PassportProvider(_passport)));
        }

        var modes = new ChannelModes().GetSupportedModes();
        modes = new string(modes.OrderBy(c => c).ToArray());
        _DataStore.Set("supported.channel.modes", modes);
        _DataStore.Set("supported.user.modes", new UserModes().GetSupportedModes());
    }

    public string[] SupportPackages { get; }

    public DateTime CreationDate => _DataStore.GetAs<DateTime>("creation");

    // Server Properties To be moved to another class later
    public string Title { get; private set; }
    public bool AnnonymousAllowed { get; } = true;
    public int ChannelCount { get; } = 0;
    public IList<ChatObject> IgnoredUsers { get; } = new List<ChatObject>();
    public IList<string> Info { get; } = new List<string>();
    public int MaxMessageLength { get; } = 512;
    public int MaxInputBytes { get; private set; } = 512;
    public int MaxOutputBytes { get; private set; } = 4096;
    public int PingInterval { get; private set; } = 180;
    public int PingAttempts { get; private set; } = 3;
    public int MaxChannels { get; private set; } = 128;
    public int MaxConnections { get; private set; } = 10000;
    public int MaxAuthenticatedConnections { get; private set; } = 1000;
    public int MaxAnonymousConnections { get; private set; } = 1000;
    public int MaxGuestConnections { get; } = 1000;
    public bool BasicAuthentication { get; private set; } = true;
    public bool AnonymousConnections { get; private set; } = true;
    public int NetInvisibleCount { get; } = 0;
    public int NetServerCount { get; } = 0;
    public int NetUserCount { get; } = 0;
    public string SecurityPackages => _securityManager.GetSupportedPackages();
    public int SysopCount { get; } = 0;
    public int UnknownConnectionCount => _socketServer.CurrentConnections - NetUserCount;
    public string RemoteIp { set; get; } = string.Empty;
    public bool DisableGuestMode { set; get; }
    public bool DisableUserRegistration { get; set; }
    public bool IsDirectoryServer { get; set; }

    public void SetMotd(string motd)
    {
        var lines = motd.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        _DataStore.SetAs(Resources.ConfigMotd, lines);
    }

    public string[] GetMotd()
    {
        return _DataStore.GetAs<List<string>>(Resources.ConfigMotd)?.ToArray() ?? Array.Empty<string>();
    }

    public void AddUser(IUser user)
    {
        _pendingNewUserQueue.Enqueue(user);
    }

    public void RemoveUser(IUser user)
    {
        // Prevent duplicate pending remove requests for the same user by tracking IDs
        if (_pendingRemoveUserSet.TryAdd(user.Id, 0))
        {
            _pendingRemoveUserQueue.Enqueue(user);
        }
    }

    public void AddChannel(IChannel channel)
    {
        Channels.Add(channel);
    }

    public void RemoveChannel(IChannel channel)
    {
        InMemoryChannelRepository.Remove(channel.GetName());
        Channels.Remove(channel);
    }

    public virtual IChannel CreateChannel(string name)
    {
        return new Channel.Channel(name);
    }

    public IUser CreateUser(IConnection connection)
    {
        return new User.User(
            connection,
            Protocols[EnumProtocolType.IRC],
            new DataRegulator(MaxInputBytes, MaxOutputBytes),
            new FloodProtectionProfile(),
            this
        );
    }

    public IList<IUser> GetUsers()
    {
        return Users;
    }


    public IUser? GetUserByNickname(string nickname)
    {
        return Users.FirstOrDefault(user => string.Compare(user.GetAddress().Nickname.Trim(), nickname, true) == 0);
    }

    public IUser? GetUserByNickname(string nickname, IUser currentUser)
    {
        if (nickname.ToUpperInvariant() == currentUser.Name.ToUpperInvariant()) return currentUser;

        return GetUserByNickname(nickname);
    }

    public IList<IUser> GetUsersByList(string nicknames, char separator)
    {
        var list = nicknames.Split(separator, StringSplitOptions.RemoveEmptyEntries).ToList();

        return GetUsersByList(list, separator);
    }

    public IList<IUser> GetUsersByList(List<string> nicknames, char separator)
    {
        return Users.Where(user =>
            nicknames.Contains(user.GetAddress().Nickname, StringComparer.InvariantCultureIgnoreCase)).ToList();
    }

    public IList<IChannel> GetChannels()
    {
        return Channels;
    }

    public string GetSupportedChannelModes()
    {
        return _DataStore.Get("supported.channel.modes");
    }

    public string GetSupportedUserModes()
    {
        return _DataStore.Get("supported.user.modes");
    }

    public IDictionary<EnumProtocolType, IProtocol> GetProtocols()
    {
        return Protocols;
    }

    public Version ServerVersion { get; set; } = new(1, 0);

    public IDataStore GetDataStore()
    {
        return _DataStore;
    }

    public virtual IChannel CreateChannel(IUser creator, string name, string key)
    {
        var channel = CreateChannel(name);
        var chanProps = (ChannelProps)channel.Props;
        chanProps.Topic.Value = name;
        chanProps.OwnerKey.Value = key;
        channel.Modes.NoExtern.ModeValue = true;
        channel.Modes.TopicOp.ModeValue = true;
        channel.Modes.UserLimit.Value = 50;
        AddChannel(channel);
        return channel;
    }

    public IChannel? GetChannelByName(string name)
    {
        return Channels.SingleOrDefault(c =>
            string.Equals(c.GetName(), name, StringComparison.InvariantCultureIgnoreCase));
    }

    public ChatObject? GetChatObject(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        switch (name.Substring(0, 1))
        {
            case "*":
            case "$":
                return this;
            case "%":
            case "#":
            case "&":
                return (ChatObject?)GetChannelByName(name);
            default:
            {
                return (ChatObject?)GetUserByNickname(name);
            }
        }
    }

    public IProtocol? GetProtocol(EnumProtocolType protocolType)
    {
        if (Protocols.TryGetValue(protocolType, out var protocol)) return protocol;
        return null;
    }

    public ISecurityManager GetSecurityManager()
    {
        return _securityManager;
    }

    public ICredentialProvider? GetCredentialManager()
    {
        return _credentialProvider;
    }

    public void Shutdown()
    {
        _cancellationTokenSource.Cancel();
        _processingTask.Wait();
    }

    public override string ToString()
    {
        return Name;
    }

    // Apollo
    public void ProcessCookie(IUser user, string name, string value)
    {
        if (name == Resources.UserPropMsnRegCookie && user.IsAuthenticated() && !user.IsRegistered())
        {
            var nickname = _passport.ValidateRegCookie(value);
            if (nickname != null)
            {
                var encodedNickname = Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(nickname));
                user.Nickname = encodedNickname;

                // Set the RealName to empty string to allow it to pass register
                user.GetAddress().RealName = string.Empty;
            }
        }
        else if (name == Resources.UserPropSubscriberInfo && user.IsAuthenticated() && user.IsRegistered())
        {
            var issuedAt = user.GetSupportPackage()?.GetCredentials()?.GetIssuedAt();
            if (!issuedAt.HasValue) return;

            var subscribedString =
                _passport.ValidateSubscriberInfo(value, issuedAt.Value);
            int.TryParse(subscribedString, out var subscribed);
            if ((subscribed & 1) == 1) ((User.User)user).GetProfile().Registered = true;
        }
        else if (name == Resources.UserPropMsnProfile && user.IsAuthenticated() && !user.IsRegistered())
        {
            int.TryParse(value, out var profileCode);
            ((User.User)user).GetProfile().SetProfileCode(profileCode);
        }
        else if (name == Resources.UserPropRole && user.IsAuthenticated())
        {
            var dict = _passport.ValidateRole(value);
            if (dict == null) return;

            if (dict.ContainsKey("umode"))
            {
                var modes = dict["umode"];
                foreach (var mode in modes)
                {
                    var userModes = (UserModes)user.Modes;
                    if (userModes.HasMode(mode)) userModes[mode].Set(true);
                    ModeRule.DispatchModeChange(mode, (IChatObject)user, (IChatObject)user, true, string.Empty);
                }
            }

            if (dict.ContainsKey("utype"))
            {
                var levelType = dict["utype"];

                switch (levelType)
                {
                    case "A":
                    {
                        user.ChangeNickname(user.Nickname, true);
                        user.PromoteToAdministrator();
                        break;
                    }
                    case "S":
                    {
                        user.ChangeNickname(user.Nickname, true);
                        user.PromoteToSysop();
                        break;
                    }
                    case "G":
                    {
                        user.ChangeNickname(user.Nickname, true);
                        user.PromoteToGuide();
                        break;
                    }
                }
            }
        }
    }

    public void LoadSettingsFromDataStore()
    {
        var title = _DataStore.Get(Resources.ConfigServerTitle);
        var maxInputBytes = _DataStore.GetAs<int>(Resources.ConfigMaxInputBytes);
        var maxOutputBytes = _DataStore.GetAs<int>(Resources.ConfigMaxOutputBytes);
        var pingInterval = _DataStore.GetAs<int>(Resources.ConfigPingInterval);
        var pingAttempts = _DataStore.GetAs<int>(Resources.ConfigPingAttempts);
        var maxChannels = _DataStore.GetAs<int>(Resources.ConfigMaxChannels);
        var maxConnections = _DataStore.GetAs<int>(Resources.ConfigMaxConnections);
        var maxAuthenticatedConnections = _DataStore.GetAs<int>(Resources.ConfigMaxAuthenticatedConnections);
        var maxAnonymousConnections = _DataStore.GetAs<int?>(Resources.ConfigMaxAnonymousConnections);
        var basicAuthentication = _DataStore.GetAs<bool?>(Resources.ConfigBasicAuthentication);
        var anonymousConnections = _DataStore.GetAs<bool?>(Resources.ConfigAnonymousConnections);

        if (!string.IsNullOrWhiteSpace(title)) Title = title;
        if (maxInputBytes > 0) MaxInputBytes = maxInputBytes;
        if (maxOutputBytes > 0) MaxOutputBytes = maxOutputBytes;
        if (pingInterval > 0) PingInterval = pingInterval;
        if (pingAttempts > 0) PingAttempts = pingAttempts;
        if (maxChannels > 0) MaxChannels = maxChannels;
        if (maxConnections > 0) MaxConnections = maxConnections;
        if (maxAuthenticatedConnections > 0) MaxAuthenticatedConnections = maxAuthenticatedConnections;
        if (maxAnonymousConnections.HasValue) MaxAnonymousConnections = maxAnonymousConnections.Value;
        if (basicAuthentication.HasValue) BasicAuthentication = basicAuthentication.Value;
        if (anonymousConnections.HasValue) AnonymousConnections = anonymousConnections.Value;
    }

    private void Process()
    {
        var backoffMs = 0;
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            var hasWork = false;

            AddPendingUsers();
            RemovePendingUsers();

            // do stuff
            foreach (var user in Users)
            {
                if (user.DisconnectIfIncomingThresholdExceeded()) continue;

                if (user.GetDataRegulator().GetIncomingBytes() > 0)
                {
                    if (ProcessNextCommand(user))
                    {
                        hasWork = true;
                        backoffMs = 0;
                    }
                }

                ProcessNextModeOperation(user);

                if (!user.DisconnectIfOutgoingThresholdExceeded()) user.Flush();
                user.DisconnectIfInactive();
            }

            if (!hasWork)
            {
                if (backoffMs < 1000) backoffMs += 10;
                Thread.Sleep(backoffMs);
            }
        }
    }

    private void AddPendingUsers()
    {
        if (_pendingNewUserQueue.Count > 0)
        {
            var addedCount = 0;
            // add new pending users
            while (_pendingNewUserQueue.TryDequeue(out var user))
            {
                user.Props.Oid.Value = "0";
                Users.Add(user);
                addedCount++;
            }

            Log.Debug($"Added {addedCount} users. Total Users = {Users.Count}");
        }
    }

    private void RemovePendingUsers()
    {
        if (_pendingRemoveUserQueue.Count > 0)
        {
            var removedCount = 0;
            // remove pending to be removed users

            while (_pendingRemoveUserQueue.TryDequeue(out var user))
            {
                // Try to remove; if removal fails because the user is already gone, don't requeue endlessly
                if (!Users.Remove(user))
                {
                    // If user already removed from Users collection, just ensure the id is removed from the pending set and skip
                    if (!Users.Any(u => u.Id == user.Id))
                    {
                        _pendingRemoveUserSet.TryRemove(user.Id, out _);
                        continue;
                    }

                    Log.Error($"Failed to remove {user}. Requeueing");
                    // Re-enqueue for retry. We keep the id in the set while retrying so duplicates won't be introduced.
                    _pendingRemoveUserQueue.Enqueue(user);
                    continue;
                }

                // Successful removal: clear the pending set entry and perform channel cleanup
                _pendingRemoveUserSet.TryRemove(user.Id, out _);
                Quit.QuitChannels(user, "Connection reset by peer");
                removedCount++;
            }

            Log.Debug($"Removed {removedCount} users. Total Users = {Users.Count}");
        }
    }

    protected void AddCommand(ICommand command)
    {
        foreach (var protocol in Protocols)
            protocol.Value.AddCommand(command, command.GetName());
    }

    protected void AddCommand(ICommand command, EnumProtocolType fromProtocol, string name)
    {
        foreach (var protocol in Protocols)
            if (protocol.Key >= fromProtocol)
                protocol.Value.AddCommand(command, name);
    }

    protected void AddProtocol(EnumProtocolType protocolType, IProtocol protocol, bool inheritCommands = true)
    {
        if (inheritCommands)
            for (var protocolIndex = 0; protocolIndex < (int)protocolType; protocolIndex++)
                if (Protocols.ContainsKey((EnumProtocolType)protocolIndex))
                    foreach (var command in Protocols[(EnumProtocolType)protocolIndex].GetCommands())
                        protocol.AddCommand(command.Value, command.Key);
        Protocols.Add(protocolType, protocol);
    }

    protected void FlushCommands()
    {
        foreach (var protocol in Protocols) protocol.Value.FlushCommands();
    }

    private void ProcessNextModeOperation(IUser user)
    {
        var modeOperations = user.GetModeOperations();
        if (modeOperations.Count > 0) modeOperations.Dequeue().Execute();
    }

    private bool ProcessNextCommand(IUser user)
    {
        var message = user.GetDataRegulator().PeekIncoming();
        if (message == null) return false;

        var command = message.GetCommand();
        if (command == null)
        {
            user.GetDataRegulator().PopIncoming();
            user.Send(Raws.IRCX_ERR_UNKNOWNCOMMAND_421(this, user, message.GetCommandName()));
            return true;
            // command not found
        }

        var floodResult = _floodProtectionManager.Audit(user.GetFloodProtectionProfile(),
            command.GetDataType(), user.GetLevel());
        if (floodResult == EnumFloodResult.Ok)
        {
            if (command is not Ping && command is not Pong) user.LastIdle = DateTime.UtcNow;

            Log.Trace($"Processing: {message.OriginalText}");

            var chatFrame = user.GetNextFrame();
            if (!command.RegistrationNeeded(chatFrame) && command.ParametersAreValid(chatFrame))
                try
                {
                    command.Execute(chatFrame);
                }
                catch (Exception e)
                {
                    chatFrame.User.Send(
                        Raws.IRC_RAW_999(chatFrame.Server, chatFrame.User, Resources.ServerError));
                    Log.Error(e.ToString());
                }

            // Check if user can register
            if (!chatFrame.User.IsRegistered()) Register.TryRegister(chatFrame);
            return true;
        }

        return false;
    }

    // IRCX 
    protected EnumChannelAccessResult CheckAuthOnly()
    {
        if (Modes.GetModeValue(Resources.ChannelModeAuthOnly) == 1)
            return EnumChannelAccessResult.ERR_AUTHONLYCHAN;
        return EnumChannelAccessResult.NONE;
    }

    protected EnumChannelAccessResult CheckSecureOnly()
    {
        // TODO: Whatever this is...
        return EnumChannelAccessResult.ERR_SECUREONLYCHAN;
    }
}

