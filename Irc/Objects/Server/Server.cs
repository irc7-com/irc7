using System.Collections.Concurrent;
using System.Reflection;
using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Extensions.Security;
using Irc.Extensions.Security.Packages;
using Irc.Factories;
using Irc.Interfaces;
using Irc.IO;
using Irc.Security;
using Irc7d;
using NLog;
using Version = System.Version;

namespace Irc.Objects.Server;

public class Server : ChatObject, IServer
{
    public static readonly NLog.Logger Log = LogManager.GetCurrentClassLogger();

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    protected readonly IDataStore _dataStore;
    private readonly IFloodProtectionManager _floodProtectionManager;
    private readonly Task _processingTask;
    private readonly ISecurityManager _securityManager;
    private readonly ISocketServer _socketServer;
    private readonly IUserFactory _userFactory;
    private readonly ConcurrentQueue<IUser> PendingNewUserQueue = new();
    private readonly ConcurrentQueue<IUser> PendingRemoveUserQueue = new();
    public IDictionary<EnumProtocolType, IProtocol> _protocols = new Dictionary<EnumProtocolType, IProtocol>();

    public IList<IChannel> Channels;

    public IList<IUser> Users = new List<IUser>();


    public Server(ISocketServer socketServer,
        ISecurityManager securityManager,
        IFloodProtectionManager floodProtectionManager,
        IDataStore dataStore,
        IList<IChannel> channels,
        IUserFactory userFactory) : base(new ModeCollection(), dataStore)
    {
        Title = Name;
        _socketServer = socketServer;
        _securityManager = securityManager;
        _floodProtectionManager = floodProtectionManager;
        _dataStore = dataStore;
        Channels = channels;
        _userFactory = userFactory;
        _processingTask = new Task(Process);
        _processingTask.Start();

        LoadSettingsFromDataStore();

        _dataStore.SetAs("creation", DateTime.UtcNow);
        _dataStore.Set("supported.channel.modes",
            new ChannelModes().GetSupportedModes());
        _dataStore.Set("supported.user.modes", new UserModes().GetSupportedModes());
        SupportPackages = _dataStore.GetAs<List<string>>(Resources.ConfigSaslPackages)?.ToArray() ?? Array.Empty<string>();

        if (MaxAnonymousConnections > 0) _securityManager.AddSupportPackage(new ANON());

        _protocols.Add(EnumProtocolType.IRC, new Irc());

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
    }

    public string[] SupportPackages { get; }

    public DateTime CreationDate => _dataStore.GetAs<DateTime>("creation");

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
    public int NetInvisibleCount { get; }
    public int NetServerCount { get; }
    public int NetUserCount { get; }
    public string SecurityPackages => _securityManager.GetSupportedPackages();
    public int SysopCount { get; }
    public int UnknownConnectionCount => _socketServer.CurrentConnections - NetUserCount;
    public string RemoteIP { set; get; }
    public bool DisableGuestMode { set; get; }
    public bool DisableUserRegistration { get; set; }

    public void SetMOTD(string motd)
    {
        var lines = motd.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        _dataStore.SetAs(Resources.ConfigMotd, lines);
    }

    public string[] GetMOTD()
    {
        return _dataStore.GetAs<List<string>>(Resources.ConfigMotd).ToArray() ?? Array.Empty<string>();
    }

    public void AddUser(IUser user)
    {
        PendingNewUserQueue.Enqueue(user);
    }

    public void RemoveUser(IUser user)
    {
        PendingRemoveUserQueue.Enqueue(user);
    }

    public void AddChannel(IChannel channel)
    {
        Channels.Add(channel);
    }

    public void RemoveChannel(IChannel channel)
    {
        Channels.Remove(channel);
    }

    public virtual IChannel CreateChannel(string name)
    {
        return new Channel.Channel(name, new ChannelModes(), new DataStore(name, "store"));
    }

    public IUser CreateUser(IConnection connection)
    {
        return _userFactory.Create(this, connection);
    }

    public IList<IUser> GetUsers()
    {
        return Users;
    }


    public IUser GetUserByNickname(string nickname)
    {
        return Users.FirstOrDefault(user => string.Compare(user.GetAddress().Nickname.Trim(), nickname, true) == 0);
    }

    public IUser GetUserByNickname(string nickname, IUser currentUser)
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
        return _dataStore.Get("supported.channel.modes");
    }

    public string GetSupportedUserModes()
    {
        return _dataStore.Get("supported.user.modes");
    }

    public IDictionary<EnumProtocolType, IProtocol> GetProtocols()
    {
        return _protocols;
    }

    public Version ServerVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0);

    public IDataStore GetDataStore()
    {
        return _dataStore;
    }

    public virtual IChannel CreateChannel(IUser creator, string name, string key)
    {
        var channel = CreateChannel(name);
        channel.ChannelStore.Set("topic", name);
        if (!string.IsNullOrEmpty(key))
        {
            channel.Modes.Key = key;
            channel.ChannelStore.Set("key", key);
        }

        channel.Modes.NoExtern = true;
        channel.Modes.TopicOp = true;
        channel.Modes.UserLimit = 50;
        AddChannel(channel);
        return channel;
    }

    public IChannel GetChannelByName(string name)
    {
        return Channels.SingleOrDefault(c =>
            string.Equals(c.GetName(), name, StringComparison.InvariantCultureIgnoreCase));
    }

    public ChatObject GetChatObject(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        switch (name.Substring(0, 1))
        {
            case "*":
            case "$":
            {
                return this;
            }
            case "%":
            case "#":
            case "&":
                return (ChatObject)GetChannelByName(name);
            default:
            {
                return (ChatObject)GetUserByNickname(name);
            }
        }
    }

    public IProtocol GetProtocol(EnumProtocolType protocolType)
    {
        if (_protocols.TryGetValue(protocolType, out var protocol)) return protocol;
        return null;
    }

    public ISecurityManager GetSecurityManager()
    {
        return _securityManager;
    }

    public ICredentialProvider? GetCredentialManager()
    {
        return null;
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

    public void LoadSettingsFromDataStore()
    {
        var title = _dataStore.Get(Resources.ConfigServerTitle);
        var maxInputBytes = _dataStore.GetAs<int>(Resources.ConfigMaxInputBytes);
        var maxOutputBytes = _dataStore.GetAs<int>(Resources.ConfigMaxOutputBytes);
        var pingInterval = _dataStore.GetAs<int>(Resources.ConfigPingInterval);
        var pingAttempts = _dataStore.GetAs<int>(Resources.ConfigPingAttempts);
        var maxChannels = _dataStore.GetAs<int>(Resources.ConfigMaxChannels);
        var maxConnections = _dataStore.GetAs<int>(Resources.ConfigMaxConnections);
        var maxAuthenticatedConnections = _dataStore.GetAs<int>(Resources.ConfigMaxAuthenticatedConnections);
        var maxAnonymousConnections = _dataStore.GetAs<int>(Resources.ConfigMaxAnonymousConnections);
        var basicAuthentication = _dataStore.GetAs<bool>(Resources.ConfigBasicAuthentication);
        var anonymousConnections = _dataStore.GetAs<bool>(Resources.ConfigAnonymousConnections);

        if (!string.IsNullOrWhiteSpace(title)) Title = title;
        if (maxInputBytes > 0) MaxInputBytes = maxInputBytes;
        if (maxOutputBytes > 0) MaxOutputBytes = maxOutputBytes;
        if (pingInterval > 0) PingInterval = pingInterval;
        if (pingAttempts > 0) PingAttempts = pingAttempts;
        if (maxChannels > 0) MaxChannels = maxChannels;
        if (maxConnections > 0) MaxConnections = maxConnections;
        if (maxAuthenticatedConnections > 0) MaxAuthenticatedConnections = maxAuthenticatedConnections;
        if (maxAnonymousConnections != null) MaxAnonymousConnections = maxAnonymousConnections;
        if (basicAuthentication != null) BasicAuthentication = basicAuthentication;
        if (anonymousConnections != null) AnonymousConnections = anonymousConnections;
    }

    private void Process()
    {
        var backoffMS = 0;
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            var hasWork = false;

            RemovePendingUsers();
            AddPendingUsers();

            // do stuff
            foreach (var user in Users)
            {
                if (user.DisconnectIfIncomingThresholdExceeded()) continue;

                if (user.GetDataRegulator().GetIncomingBytes() > 0)
                {
                    hasWork = true;
                    backoffMS = 0;

                    ProcessNextCommand(user);
                }

                ProcessNextModeOperation(user);

                if (!user.DisconnectIfOutgoingThresholdExceeded()) user.Flush();
                user.DisconnectIfInactive();
            }

            if (!hasWork)
            {
                if (backoffMS < 1000) backoffMS += 10;
                Thread.Sleep(backoffMS);
            }
        }
    }

    private void AddPendingUsers()
    {
        if (PendingNewUserQueue.Count > 0)
        {
            // add new pending users
            foreach (var user in PendingNewUserQueue)
            {
                user.GetDataStore().Set(Resources.UserPropOid, "0");
                Users.Add(user);
            }

            Log.Debug($"Added {PendingNewUserQueue.Count} users. Total Users = {Users.Count}");
            PendingNewUserQueue.Clear();
        }
    }

    private void RemovePendingUsers()
    {
        if (PendingRemoveUserQueue.Count > 0)
        {
            // remove pending to be removed users

            foreach (var user in PendingRemoveUserQueue)
            {
                if (!Users.Remove(user))
                {
                    Log.Error($"Failed to remove {user}. Requeueing");
                    PendingRemoveUserQueue.Enqueue(user);
                    continue;
                }

                Quit.QuitChannels(user, "Connection reset by peer");
            }

            Log.Debug($"Removed {PendingRemoveUserQueue.Count} users. Total Users = {Users.Count}");
            PendingRemoveUserQueue.Clear();
        }
    }

    protected void AddCommand(ICommand command)
    {
        foreach (var protocol in _protocols)
                protocol.Value.AddCommand(command, command.GetName());
    }

    protected void AddCommand(ICommand command, EnumProtocolType fromProtocol, string name)
    {
        foreach (var protocol in _protocols)
            if (protocol.Key >= fromProtocol)
                protocol.Value.AddCommand(command, name);
    }

    protected void AddProtocol(EnumProtocolType protocolType, IProtocol protocol, bool inheritCommands = true)
    {
        if (inheritCommands)
            for (var protocolIndex = 0; protocolIndex < (int)protocolType; protocolIndex++)
                if (_protocols.ContainsKey((EnumProtocolType)protocolIndex))
                    foreach (var command in _protocols[(EnumProtocolType)protocolIndex].GetCommands())
                        protocol.AddCommand(command.Value, command.Key);
        _protocols.Add(protocolType, protocol);
    }

    protected void FlushCommands()
    {
        foreach (var protocol in _protocols) protocol.Value.FlushCommands();
    }

    private void ProcessNextModeOperation(IUser user)
    {
        var modeOperations = user.GetModeOperations();
        if (modeOperations.Count > 0) modeOperations.Dequeue().Execute();
    }

    private void ProcessNextCommand(IUser user)
    {
        var message = user.GetDataRegulator().PeekIncoming();
        if (message == null) return;

        var command = message.GetCommand();
        if (command != null)
        {
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
                            IrcRaws.IRC_RAW_999(chatFrame.Server, chatFrame.User, Resources.ServerError));
                        Log.Error(e.ToString());
                    }

                // Check if user can register
                if (!chatFrame.User.IsRegistered()) Register.TryRegister(chatFrame);
            }
        }
        else
        {
            user.GetDataRegulator().PopIncoming();
            user.Send(Raw.IRCX_ERR_UNKNOWNCOMMAND_421(this, user, message.GetCommandName()));
            // command not found
        }
    }
}