using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Irc.Objects.Channel;
using StackExchange.Redis;

namespace Irc.Services;

public class CacheManager
{
    private readonly ConnectionMultiplexer? _redis;
    private readonly IDatabase? _db;
    public readonly ISubscriber? Subscriber;

    public CacheManager(string? redisUrl)
    {
        if (string.IsNullOrEmpty(redisUrl)) return;
        
        try
        {
            _redis = ConnectionMultiplexer.Connect(redisUrl);
            _db = _redis.GetDatabase();
            Subscriber = _redis.GetSubscriber();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CacheManager] Failed to connect to Redis/KeyDB at {redisUrl}: {ex.Message}");
        }
    }

    public bool IsConnected => _redis?.IsConnected ?? false;

    // Registers the ACS server
    public void RegisterServer(string serverId, string ip, int port, string name, int usersOnline)
    {
        if (_db == null) return;
        
        var payload = JsonSerializer.Serialize(new
        {
            Ip = ip,
            Port = port,
            Name = name,
            UsersOnline = usersOnline
        });

        // Use a 30-second TTL
        _db.StringSet($"acs:server:{serverId}", payload, TimeSpan.FromSeconds(10));
    }

    // Unregisters the ACS server
    public void UnregisterServer(string serverId)
    {
        _db?.KeyDelete($"acs:server:{serverId}");
    }

    public bool RegisterRoom(InMemoryChannel inMemoryChannel, string serverId)
    {
        return RegisterRoom(
            roomName: inMemoryChannel.ChannelName,
            serverId: serverId,
            category: inMemoryChannel.Category,
            // Not sure what this is since we have roomName
            name: inMemoryChannel.ChannelName,
            topic: inMemoryChannel.ChannelTopic,
            modes: inMemoryChannel.Modes,
            // Need to sort out the below better
            managed: inMemoryChannel.Modes.Contains('r'),
            locale: inMemoryChannel.Locale,
            language: inMemoryChannel.Language.ToString(),
            // Current users would always be 0 here as we are registering a room
            currentUsers: 0,
            // Need to sort this out better
            maxUsers: 50,
            ownerKey: inMemoryChannel.OwnerKey,
            hostKey: inMemoryChannel.HostKey
        );
    }

    // Registers a room to a specific ACS
    public bool RegisterRoom(string roomName, string serverId, string category, string name, string topic, string modes, bool managed, string locale, string language, int currentUsers, int maxUsers, string ownerKey = "", string hostKey = "")
    {
        if (_db == null) return true;

        var payload = JsonSerializer.Serialize(new AcsRoomInfo
        {
            ServerId = serverId,
            Category = category,
            Name = name,
            Topic = topic,
            Modes = modes,
            Managed = managed,
            Locale = locale,
            Language = language,
            CurrentUsers = currentUsers,
            MaxUsers = maxUsers,
            OwnerKey = ownerKey,
            HostKey = hostKey
        });

        var script = @"
            local exists = redis.call('HEXISTS', 'acs:rooms', ARGV[1])
            if exists == 0 then
                redis.call('HSET', 'acs:rooms', ARGV[1], ARGV[2])
                return 1
            else
                local current = redis.call('HGET', 'acs:rooms', ARGV[1])
                local decoded = cjson.decode(current)
                if decoded.serverId == ARGV[3] then
                    redis.call('HSET', 'acs:rooms', ARGV[1], ARGV[2])
                    return 1
                else
                    local server_key = 'acs:server:' .. decoded.serverId
                    local server_exists = redis.call('EXISTS', server_key)
                    if server_exists == 0 then
                        redis.call('HSET', 'acs:rooms', ARGV[1], ARGV[2])
                        return 1
                    end
                end
                return 0
            end
        ";

        try 
        {
            var result = (int)_db.ScriptEvaluate(script, keys: null, values: new RedisValue[] { roomName.ToUpper(), payload, serverId });
            return result == 1;
        } 
        catch (Exception ex) 
        {
            Console.WriteLine($"[CacheManager] Failed to run LUA script on Redis: {ex.Message}");
            return false;
        }
    }

    // Unregisters a room
    public void UnregisterRoom(string roomName)
    {
        _db?.HashDelete("acs:rooms", roomName);
    }

    // Gets the server ID for a given room
    public string? GetServerForRoom(string roomName)
    {
        if (_db == null) return null;
        
        var value = _db.HashGet("acs:rooms", roomName.ToUpper());
        if (value.HasValue)
        {
            var roomInfo = JsonSerializer.Deserialize<AcsRoomInfo>(value.ToString());
            return roomInfo?.ServerId;
        }
        return null;
    }

    public AcsRoomInfo? GetRoomInfo(string roomName)
    {
        if (_db == null) return null;
        
        var value = _db.HashGet("acs:rooms", roomName.ToUpper());
        if (value.HasValue)
        {
            return JsonSerializer.Deserialize<AcsRoomInfo>(value.ToString());
        }
        return null;
    }

    public IEnumerable<AcsRoomInfo> GetRoomsForServer(string serverId)
    {
        if (_db == null) yield break;

        var entries = _db.HashGetAll("acs:rooms");
        foreach (var entry in entries)
        {
            if (entry.Value.HasValue)
            {
                var roomInfo = JsonSerializer.Deserialize<AcsRoomInfo>(entry.Value.ToString());
                if (roomInfo != null && roomInfo.ServerId == serverId)
                {
                    yield return roomInfo;
                }
            }
        }
    }

    // Gets all active ACS servers
    public IEnumerable<AcsServerInfo> GetActiveServers()
    {
        if (_redis == null || _db == null) yield break;

        var endpoints = _redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _redis.GetServer(endpoint);
            var keys = server.Keys(pattern: "acs:server:*");

            foreach (var key in keys)
            {
                var value = _db.StringGet(key);
                if (value.HasValue)
                {
                    var info = JsonSerializer.Deserialize<AcsServerInfo>(value.ToString());
                    if (info != null)
                    {
                        info.ServerId = key.ToString().Substring("acs:server:".Length);
                        yield return info;
                    }
                }
            }
        }
    }

    public void PublishChannelCreate(string serverId, string payload)
    {
        if (Subscriber == null) return;
        try
        {
            var channelName = new RedisChannel($"acs:events:channels:{serverId}", RedisChannel.PatternMode.Literal);
            Console.WriteLine($"[CacheManager] Publishing to PubSub channel {channelName}");
            Subscriber.Publish(channelName, payload);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CacheManager] Failed to publish channel create: {ex.Message}");
        }
    }

    public void StartConsumingEvents(string serverId, Action<string> onMessageReceived, CancellationToken cancellationToken = default)
    {
        if (Subscriber == null) return;
        var channelName = new RedisChannel($"acs:events:channels:{serverId}", RedisChannel.PatternMode.Literal);

        Console.WriteLine($"[CacheManager] Subscribing to PubSub channel {channelName}");

        try
        {
            Subscriber.Subscribe(channelName, (channel, value) =>
            {
                if (value.HasValue)
                {
                    Console.WriteLine($"[CacheManager] Received event on {channelName}");
                    onMessageReceived(value.ToString());
                }
            });

            cancellationToken.Register(() =>
            {
                try
                {
                    Subscriber.Unsubscribe(channelName);
                }
                catch { }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CacheManager] Failed to subscribe to channel: {ex.Message}");
        }
    }
}

public class AcsServerInfo
{
    public string ServerId { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UsersOnline { get; set; }
}

public class AcsRoomInfo
{
    [JsonPropertyName("serverId")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("modes")]
    public string Modes { get; set; } = string.Empty;

    [JsonPropertyName("managed")]
    public bool Managed { get; set; }

    [JsonPropertyName("locale")]
    public string Locale { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("currentUsers")]
    public int CurrentUsers { get; set; }

    [JsonPropertyName("maxUsers")]
    public int MaxUsers { get; set; }

    [JsonPropertyName("ownerKey")]
    public string OwnerKey { get; set; } = string.Empty;

    [JsonPropertyName("hostKey")]
    public string HostKey { get; set; } = string.Empty;

    public InMemoryChannel ToInMemoryChannel()
    {
        var language = 1;
        if (!string.IsNullOrWhiteSpace(Language) && int.TryParse(Language, out var parsedLanguage))
        {
            language = parsedLanguage;
        }

        return new InMemoryChannel
        {
            ServerName = ServerId,
            Category = Category,
            ChannelName = Name,
            ChannelTopic = Topic,
            Modes = Modes,
            UserLimit = MaxUsers > 0 ? MaxUsers : 50,
            Locale = Locale,
            Language = language,
            OwnerKey = OwnerKey,
            HostKey = HostKey
        };
    }
}