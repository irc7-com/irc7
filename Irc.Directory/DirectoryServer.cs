using Irc.Commands;
using Irc.Directory.Commands;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Server;
using Nick = Irc.Directory.Commands.Nick;
using Version = Irc.Commands.Version;

namespace Irc.Directory;

public class DirectoryServer : Server
{
    public readonly string ChatServerIp = string.Empty;
    public readonly int ChatServerPort;

    public Irc.Services.AcsServerInfo? GetTargetServerForRoom(string roomName)
    {
        if (!CacheManager.IsConnected) return null;

        var activeServers = CacheManager.GetActiveServers().ToList();
        var roomInfo = CacheManager.GetRoomInfo(roomName);
        
        Irc.Services.AcsServerInfo? targetServer = null;

        if (roomInfo != null && !string.IsNullOrEmpty(roomInfo.ServerId))
        {
            targetServer = activeServers.FirstOrDefault(s => s.ServerId == roomInfo.ServerId);
            
            // Failover condition: Room exists in Redis but its assigned server is dead
            if (targetServer == null)
            {
                targetServer = activeServers.OrderBy(s => s.UsersOnline).FirstOrDefault();
                if (targetServer != null)
                {
                    // Clone the room to the new server via PubSub
                    var inMemoryChannel = roomInfo.ToInMemoryChannel();
                    inMemoryChannel.ServerName = targetServer.Name;
                    
                    if (CacheManager.Subscriber != null)
                    {
                        CacheManager.PublishChannelCreate(targetServer.ServerId, System.Text.Json.JsonSerializer.Serialize(inMemoryChannel));
                        
                        // Update the room immediately in Redis so we don't cause an infinite failover loop 
                        // for concurrent requests while the ACS is booting up the room.
                        CacheManager.RegisterRoom(
                            roomName: roomInfo.Name,
                            serverId: targetServer.ServerId,
                            category: roomInfo.Category,
                            name: roomInfo.Name,
                            topic: roomInfo.Topic,
                            modes: roomInfo.Modes,
                            managed: roomInfo.Managed,
                            locale: roomInfo.Locale,
                            language: roomInfo.Language,
                            currentUsers: roomInfo.CurrentUsers,
                            maxUsers: roomInfo.MaxUsers,
                            ownerKey: roomInfo.OwnerKey,
                            hostKey: roomInfo.HostKey
                        );
                    }
                }
            }
        }
        else
        {
            // Room does not exist, load balance to server with least connections
            targetServer = activeServers.OrderBy(s => s.UsersOnline).FirstOrDefault();
        }

        return targetServer;
    }

    public DirectoryServer(
        ISocketServer socketServer,
        ISecurityManager securityManager,
        IFloodProtectionManager floodProtectionManager,
        IDataStore dataStore,
        IList<IChannel> channels,
        ICredentialProvider? ntlmCredentialProvider = null,
        string? chatServerIp = null,
        string? redisUrl = null
    )
        : base(
            socketServer,
            securityManager,
            floodProtectionManager,
            dataStore,
            channels,
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
