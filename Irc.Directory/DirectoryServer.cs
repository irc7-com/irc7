using Irc.Commands;
using Irc.Directory.Commands;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Irc.Objects.Server;
using Irc.Services;
using Nick = Irc.Directory.Commands.Nick;
using Version = Irc.Commands.Version;

namespace Irc.Directory;

public class DirectoryServer : Server
{
    public readonly string ChatServerIp = string.Empty;
    public readonly int ChatServerPort;

    public enum FindChannelStatus
    {
        NotFound,
        Found,
        Down
    }

    private static InvalidOperationException ChannelManipulationNotSupported() =>
        new("Channel manipulation is not supported on a DirectoryServer.");

    public override bool AddChannel(IChannel channel) => throw ChannelManipulationNotSupported();

    public override void RemoveChannel(IChannel channel) => throw ChannelManipulationNotSupported();

    public override IChannel? CreateChannel(string name) => throw ChannelManipulationNotSupported();

    public override IChannel? CreateChannel(string name, string topic, string key) => throw ChannelManipulationNotSupported();

    public AcsServerInfo? FindChannel(string roomName)
    {
        return FindChannel(roomName, out _);
    }

    public AcsServerInfo? FindChannel(string roomName, out FindChannelStatus status)
    {
        status = FindChannelStatus.NotFound;

        if (!CacheManager.IsConnected)
        {
            status = FindChannelStatus.Down;
            return null;
        }

        var activeServers = CacheManager.GetActiveServers().ToList();
        if (activeServers.Count == 0)
        {
            status = FindChannelStatus.Down;
            return null;
        }

        var roomInfo = CacheManager.GetRoomInfo(roomName);

        // If room doesn't exist return null
        if (roomInfo == null)
        {
            status = FindChannelStatus.NotFound;
            return null;
        }

        // If room exists, return the server it's assigned to
        var targetServer = LookupChannel(activeServers, roomInfo);
        if (targetServer != null)
        {
            status = FindChannelStatus.Found;
            return targetServer;
        }

        targetServer = RegisterChannel(activeServers, roomInfo.ToInMemoryChannel());
        if (targetServer == null)
        {
            // Room exists but no server could be assigned (e.g., all servers down)
            status = FindChannelStatus.Down;
            return null;
        }

        status = FindChannelStatus.Found;
        return targetServer;
    }

    private AcsServerInfo? LookupChannel(List<AcsServerInfo> activeServers, AcsRoomInfo? roomInfo)
    {
        if (roomInfo == null) return null;
        
        // If room is already assigned to a server, return it
        if (string.IsNullOrWhiteSpace(roomInfo.ServerId)) return null;
        
        AcsServerInfo? targetServer = activeServers.FirstOrDefault(s => s.ServerId == roomInfo.ServerId);
        
        // If room is assigned to a server, return it
        if (targetServer != null) return targetServer;
            
        // TBD: Probably sending something like this would be helpful
        // IRCX_RPL_FINDS_DOWN_703
        return null;
    }

    public AcsServerInfo? RegisterChannel(List<AcsServerInfo> activeServers, InMemoryChannel inMemoryChannel)
    {
        // If Redis is not available, return null
        if (CacheManager.Subscriber == null) return null;
        
        // Get the server with the fewest users and assign the room to it
        var targetServer = activeServers.OrderBy(s => s.UsersOnline).FirstOrDefault();
        if (targetServer == null) return null;
        
        // Clone the room to the new server via PubSub
        inMemoryChannel.ServerName = targetServer.Name;
                        
        // Update the room immediately in Redis so we don't cause an infinite failover loop 
        // for concurrent requests while the ACS is booting up the room.
        CacheManager.RegisterRoom(inMemoryChannel, targetServer.ServerId);
        CacheManager.PublishChannelCreate(targetServer.ServerId, System.Text.Json.JsonSerializer.Serialize(inMemoryChannel));

        return targetServer;
    }

    public DirectoryServer(
        ISocketServer socketServer,
        ISecurityManager securityManager,
        IFloodProtectionManager floodProtectionManager,
        IDataStore dataStore,
        ICredentialProvider? ntlmCredentialProvider = null,
        string? chatServerIp = null,
        string? redisUrl = null
    )
        : base(
            socketServer,
            securityManager,
            floodProtectionManager,
            dataStore,
            ntlmCredentialProvider,
            redisUrl
        )
    {
        DisableGuestMode = true;
        DisableUserRegistration = true;
        IsDirectoryServer = true;

        if (!string.IsNullOrEmpty(chatServerIp))
        {
            var parts = chatServerIp.Split(':');
            if (parts.Length > 0)
                ChatServerIp = parts[0];
            if (parts.Length > 1)
                int.TryParse(parts[1], out ChatServerPort);
            
            // If port was omitted assume 6667
            if (ChatServerPort == 0) ChatServerPort = 6667;
        }

        FlushCommands();
        AddCommand(new Ircvers());
        AddCommand(new Auth());
        AddCommand(new AuthX());
        AddCommand(new Pass());
        AddCommand(new Nick());
        AddCommand(new UserCommand(), EnumProtocolType.IRC, "User");
        AddCommand(new Finds());
        AddCommand(new Prop());
        AddCommand(new Irc.Directory.Commands.Create());
        AddCommand(new Version());
        AddCommand(new WebIrc());
    }
}
