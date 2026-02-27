using System.Text.Json;
using StackExchange.Redis;

namespace Irc.Services;

public class CacheManager
{
    private readonly ConnectionMultiplexer? _redis;
    private readonly IDatabase? _db;

    public CacheManager(string? redisUrl)
    {
        if (string.IsNullOrEmpty(redisUrl)) return;
        
        try
        {
            _redis = ConnectionMultiplexer.Connect(redisUrl);
            _db = _redis.GetDatabase();
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
        _db.StringSet($"acs:server:{serverId}", payload, TimeSpan.FromSeconds(30));
    }

    // Unregisters the ACS server
    public void UnregisterServer(string serverId)
    {
        _db?.KeyDelete($"acs:server:{serverId}");
    }

    // Registers a room to a specific ACS
    public bool RegisterRoom(string roomName, string serverId, string category, string topic, int userCount)
    {
        if (_db == null) return true;

        var payload = JsonSerializer.Serialize(new
        {
            ServerId = serverId,
            Category = category,
            Topic = topic,
            UserCount = userCount
        });

        var script = @"
            local exists = redis.call('HEXISTS', 'acs:rooms', ARGV[1])
            if exists == 0 then
                redis.call('HSET', 'acs:rooms', ARGV[1], ARGV[2])
                return 1
            else
                local current = redis.call('HGET', 'acs:rooms', ARGV[1])
                local decoded = cjson.decode(current)
                if decoded.ServerId == ARGV[3] then
                    redis.call('HSET', 'acs:rooms', ARGV[1], ARGV[2])
                    return 1
                end
                return 0
            end
        ";

        try 
        {
            var result = (int)_db.ScriptEvaluate(script, keys: null, values: new RedisValue[] { roomName, payload, serverId });
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
        
        var value = _db.HashGet("acs:rooms", roomName);
        if (value.HasValue)
        {
            var roomInfo = JsonSerializer.Deserialize<AcsRoomInfo>(value.ToString());
            return roomInfo?.ServerId;
        }
        return null;
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
    public string ServerId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public int UserCount { get; set; }
}