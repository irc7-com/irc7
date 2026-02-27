using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net;
using System.Reflection;
using System.Text.Json;
using Irc.Directory;
using Irc.Interfaces;
using Irc.IO;
using Irc.Logging;
using Irc.Objects.Server;
using Irc.Security;
using Irc.Security.Credentials;
using NLog;

namespace Irc7d;

internal class Program
{
    public static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static IServer? _server;
    private static CancellationTokenSource _cancellationTokenSource = new();

    private static async Task<int> Main(string[] args)
    {
        Logging.Attach();
        var (rootCommand, optionsDictionary) = CreateRootCommand();

        rootCommand.SetHandler(async context =>
        {
            var options = GetOptions(context, optionsDictionary);

            var ip = !string.IsNullOrEmpty(options.BindIp) ? IPAddress.Parse(options.BindIp) : IPAddress.Any;

            Enum.TryParse<IrcType>(options.ServerType, true, out var serverType);

            var socketServer = new SocketServer(ip, options.BindPort, options.Backlog, options.MaxConnections, options.MaxConnectionsPerIp,
                options.BufferSize);
            socketServer.OnListen += (_, __) => DisplayStartupInfo(options, ip);

            var credentialProvider = await LoadCredentials();

            _server = ConfigureServer(serverType, socketServer, credentialProvider, options.ChatServerIp, options.RedisUrl);

            _server.ServerVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
            _server.RemoteIp = options.Fqdn ?? "localhost";

            InitializeDefaultChannels(_server, serverType);

            if (_server is Server baseServer)
            {
                baseServer.SetupHeartbeat();
            }

            _cancellationTokenSource = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Console.CancelKeyPress += CurrentDomain_ProcessExit;
            await Task.Delay(-1, _cancellationTokenSource.Token).ContinueWith(t => { });
        });

        return await rootCommand.InvokeAsync(args);
    }

    private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        Log.Info("Shutting down...");
        _server?.Shutdown();
        _cancellationTokenSource.Cancel();
    }

    private static (RootCommand, Dictionary<string, Option>) CreateRootCommand()
    {
        var rootCommand = new RootCommand("irc7 daemon")
        {
            Name = AppDomain.CurrentDomain.FriendlyName
        };

        var configOption =
            new Option<string>(["-c", "--config"], "The path to the configuration json file (default ./config.json)")
                { ArgumentHelpName = "configfile" };
        configOption.SetDefaultValue("./config.json");
        var bindIpOption = new Option<string>(["-i", "--ip"], "The ip for the server to bind on (default 0.0.0.0)")
            { ArgumentHelpName = "bindip" };
        bindIpOption.SetDefaultValue("0.0.0.0");
        var bindPortOption = new Option<int>(["-p", "--port"], "The port for the server to bind on (default 6667)")
            { ArgumentHelpName = "bindport" };
        bindPortOption.SetDefaultValue(6667);
        var backlogOption = new Option<int>(["-k", "--backlog"], "The backlog for connecting sockets (default 512)")
            { ArgumentHelpName = "backlogsize" };
        backlogOption.SetDefaultValue(512);
        var bufferSizeOption = new Option<int>(["-z", "--buffer"], "The incoming buffer size in bytes (default 512)")
            { ArgumentHelpName = "buffersize" };
        bufferSizeOption.SetDefaultValue(512);
        
        var maxConnectionsOption =
            new Option<int>(["-m", "--maxConn"], "The maximum overall connections that can connect (default 1000)")
                { ArgumentHelpName = "maxconnections" };
        maxConnectionsOption.SetDefaultValue(1000);
        
        var maxConnectionsPerIpOption =
            new Option<int>(["-mip", "--maxConnPerIp"], "The maximum connections per IP that can connect (default 128)")
                { ArgumentHelpName = "maxconnectionsperip" };
        maxConnectionsPerIpOption.SetDefaultValue(128);
        
        var fqdnOption = new Option<string>(["-f", "--fqdn"], "The FQDN of the machine (default localhost)")
            { ArgumentHelpName = "fqdn" };
        fqdnOption.SetDefaultValue("localhost");
        var serverTypeOption = new Option<string>(["-t", "--type"], "Type of server e.g. IRC, IRCX, ACS, ADS")
            { ArgumentHelpName = "type" };
        serverTypeOption.SetDefaultValue("ACS");
        var chatServerIpOption =
            new Option<string>(["-s", "--server"], "The Chat Server Ip and Port e.g. 127.0.0.1:6667")
                { ArgumentHelpName = "server" };
        var redisUrlOption =
            new Option<string>(["-r", "--redis"], "The Redis/KeyDB connection string (optional, enables caching and ADS/ACS load balancing)")
                { ArgumentHelpName = "redisurl" };

        var options = new Dictionary<string, Option>
        {
            { "config", configOption },
            { "bindIp", bindIpOption },
            { "bindPort", bindPortOption },
            { "backlog", backlogOption },
            { "bufferSize", bufferSizeOption },
            { "maxConnections", maxConnectionsOption },
            { "maxConnectionsPerIp", maxConnectionsPerIpOption },
            { "fqdn", fqdnOption },
            { "serverType", serverTypeOption },
            { "chatServerIp", chatServerIpOption },
            { "redisUrl", redisUrlOption }
        };

        foreach (var option in options.Values) rootCommand.AddOption(option);

        return (rootCommand, options);
    }

    private static ServerOptions GetOptions(InvocationContext context, Dictionary<string, Option> optionsDict)
    {
        return new ServerOptions
        {
            ConfigPath = context.ParseResult.GetValueForOption((Option<string>)optionsDict["config"]),
            BindIp = context.ParseResult.GetValueForOption((Option<string>)optionsDict["bindIp"]),
            BindPort = context.ParseResult.GetValueForOption((Option<int>)optionsDict["bindPort"]),
            Backlog = context.ParseResult.GetValueForOption((Option<int>)optionsDict["backlog"]),
            BufferSize = context.ParseResult.GetValueForOption((Option<int>)optionsDict["bufferSize"]),
            MaxConnections = context.ParseResult.GetValueForOption((Option<int>)optionsDict["maxConnections"]),
            MaxConnectionsPerIp = context.ParseResult.GetValueForOption((Option<int>)optionsDict["maxConnectionsPerIp"]),
            Fqdn = context.ParseResult.GetValueForOption((Option<string>)optionsDict["fqdn"]),
            ServerType = context.ParseResult.GetValueForOption((Option<string>)optionsDict["serverType"]),
            ChatServerIp = context.ParseResult.GetValueForOption((Option<string>)optionsDict["chatServerIp"]),
            RedisUrl = context.ParseResult.GetValueForOption((Option<string>)optionsDict["redisUrl"])
        };
    }

    private static Server ConfigureServer(IrcType serverType, SocketServer socketServer,
        NtlmCredentials credentialProvider, string? chatServerIp, string? redisUrl)
    {
        var floodProtectionManager = new FloodProtectionManager();
        var securityManager = new SecurityManager();
        var dataStoreServerConfig = new DataStore("DefaultServer.json");
        var channels = new List<IChannel>();
        return serverType switch
        {
            IrcType.ADS => ConfigureDirectoryServer(socketServer, credentialProvider, securityManager,
                floodProtectionManager, dataStoreServerConfig, channels, chatServerIp, redisUrl),
            _ => new Server(socketServer, securityManager, floodProtectionManager, dataStoreServerConfig,
                channels, credentialProvider, redisUrl)
        };
    }

    private static DirectoryServer ConfigureDirectoryServer(SocketServer socketServer,
        NtlmCredentials credentialProvider, SecurityManager securityManager,
        FloodProtectionManager floodProtectionManager, DataStore dataStoreServerConfig, List<IChannel> channels,
        string? chatServerIp, string? redisUrl)
    {
        var server = new DirectoryServer(socketServer, securityManager, floodProtectionManager, dataStoreServerConfig,
            channels, credentialProvider, chatServerIp, redisUrl);

        return server;
    }

    private static async Task<NtlmCredentials> LoadCredentials()
    {
        if (File.Exists("DefaultCredentials.json"))
        {
            var credentials =
                JsonSerializer.Deserialize<Dictionary<string, Credential>>(
                    await File.ReadAllTextAsync("DefaultCredentials.json")) ?? new Dictionary<string, Credential>();
            return new NtlmCredentials(credentials);
        }

        return new NtlmCredentials(new Dictionary<string, Credential>());
    }

    private static void InitializeDefaultChannels(IServer server, IrcType serverType)
    {
        var defaultChannels =
            JsonSerializer.Deserialize<List<DefaultChannel>>(File.ReadAllText("DefaultChannels.json"));
        if (defaultChannels == null) return;

        foreach (var defaultChannel in defaultChannels)
        {
            var name = $"%#{defaultChannel.Name}";

            // If we're an ACS and connected to Redis, check if another ACS already hosts this channel
            if (server.IsChannelHostedElsewhere(name, out var existingServerId))
            {
                Log.Info($"Skipping default channel {name}, already hosted on {existingServerId}");
                continue;
            }

            var channel = server.CreateChannel(name);
            if (channel == null)
            {
                Log.Info($"Skipping default channel {name}, could not create channel (maybe race condition?)");
                continue;
            }
            channel.Store = true;

            channel.Props.Topic.Value = defaultChannel.Topic;
            foreach (var keyValuePair in defaultChannel.Modes)
                channel.Modes.SetModeValue(keyValuePair.Key, keyValuePair.Value);

            foreach (var keyValuePair in defaultChannel.Props)
            {
                var prop = channel.Props.GetProp(keyValuePair.Key);
                prop?.SetValue(keyValuePair.Value);
            }

            server.AddChannel(channel);
        }
    }

    private static void DisplayStartupInfo(ServerOptions options, IPAddress ip)
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║            IRC7 Server Info            ║");
        Console.WriteLine("╠════════════════════════════════════════╣");

        var infoLines = new List<string>
        {
            $"║ Server Version: {Assembly.GetExecutingAssembly().GetName().Version}",
            $"║ Listening on IP: {ip}",
            $"║ Port: {options.BindPort}",
            $"║ Max Connections: {options.MaxConnections}",
            $"║ Max Connections Per IP: {options.MaxConnectionsPerIp}",
            $"║ Server Type: {options.ServerType?.ToUpper()}",
            $"║ FQDN: {options.Fqdn}",
            $"║ Buffer Size: {options.BufferSize} bytes",
            $"║ Backlog Size: {options.Backlog}",
            $"║ Redis URL: {options.RedisUrl}"
        };
        if (!string.IsNullOrEmpty(options.ChatServerIp)) infoLines.Add($"║ Chat Server Ip: {options.ChatServerIp}");

        var maxLength = 0;
        foreach (var line in infoLines)
        {
            maxLength = Math.Max(maxLength, line.Length);
            maxLength = maxLength < 40 ? 40 : maxLength;
        }

        foreach (var line in infoLines)
        {
            var spacesToAdd = maxLength - line.Length;
            var formattedLine = line + new string(' ', spacesToAdd) + " ║";
            Console.WriteLine(formattedLine);
        }

        Console.WriteLine("╚════════════════════════════════════════╝");
    }

    private enum IrcType
    {
        ACS,
        ADS
    }
}