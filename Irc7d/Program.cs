using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Irc.Extensions.Apollo.Directory;
using Irc.Extensions.Apollo.Factories;
using Irc.Extensions.Apollo.Objects.Server;
using Irc.Extensions.Factories;
using Irc.Extensions.Objects.Channel;
using Irc.Extensions.Objects.Server;
using Irc.Extensions.Security.Credentials;
using Irc.Factories;
using Irc.Interfaces;
using Irc.IO;
using Irc.Logger;
using Irc.Objects.Server;
using Irc.Security;
using NLog;

namespace Irc7d;

internal class Program
{
    public static readonly NLog.Logger Log = LogManager.GetCurrentClassLogger();

    private static IServer? _server;
    private static CancellationTokenSource _cancellationTokenSource = new();

    private static async Task<int> Main(string[] args)
    {
        Logging.Attach();
        var (rootCommand, optionsDictionary) = CreateRootCommand();

        rootCommand.SetHandler(async (InvocationContext context) =>
        {
            var options = GetOptions(context, optionsDictionary);

            var ip = !string.IsNullOrEmpty(options.BindIp) ? IPAddress.Parse(options.BindIp) : IPAddress.Any;

            Enum.TryParse<IrcType>(options.ServerType, true, out var serverType);

            var socketServer = new SocketServer(ip, options.BindPort, options.Backlog, options.MaxConnections, options.BufferSize);
            socketServer.OnListen += (_, __) => DisplayStartupInfo(options, ip);

            var credentialProvider = await LoadCredentials();

            _server = ConfigureServer(serverType, socketServer, credentialProvider, options.ChatServerIp);

            _server.ServerVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
            _server.RemoteIp = options.Fqdn ?? "localhost";

            InitializeDefaultChannels(_server, serverType);

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

        var configOption = new Option<string>(["-c", "--config"], "The path to the configuration json file (default ./config.json)") { ArgumentHelpName = "configfile" };
        configOption.SetDefaultValue("./config.json");
        var bindIpOption = new Option<string>(["-i", "--ip"], "The ip for the server to bind on (default 0.0.0.0)") { ArgumentHelpName = "bindip" };
        bindIpOption.SetDefaultValue("0.0.0.0");
        var bindPortOption = new Option<int>(["-p", "--port"], "The port for the server to bind on (default 6667)") { ArgumentHelpName = "bindport" };
        bindPortOption.SetDefaultValue(6667);
        var backlogOption = new Option<int>(["-k", "--backlog"], "The backlog for connecting sockets (default 512)") { ArgumentHelpName = "backlogsize" };
        backlogOption.SetDefaultValue(512);
        var bufferSizeOption = new Option<int>(["-z", "--buffer"], "The incoming buffer size in bytes (default 512)") { ArgumentHelpName = "buffersize" };
        bufferSizeOption.SetDefaultValue(512);
        var maxConnectionsPerIpOption = new Option<int>(["-m", "--maxConn"], "The maximum connections per IP that can connect (default 128)") { ArgumentHelpName = "maxconnections" };
        maxConnectionsPerIpOption.SetDefaultValue(128);
        var fqdnOption = new Option<string>(["-f", "--fqdn"], "The FQDN of the machine (default localhost)") { ArgumentHelpName = "fqdn" };
        fqdnOption.SetDefaultValue("localhost");
        var serverTypeOption = new Option<string>(["-t", "--type"], "Type of server e.g. IRC, IRCX, ACS, ADS") { ArgumentHelpName = "type" };
        serverTypeOption.SetDefaultValue("ACS");
        var chatServerIpOption = new Option<string>(["-s", "--server"], "The Chat Server Ip and Port e.g. 127.0.0.1:6667") { ArgumentHelpName = "server" };

        var options = new Dictionary<string, Option>
        {
            { "config", configOption },
            { "bindIp", bindIpOption },
            { "bindPort", bindPortOption },
            { "backlog", backlogOption },
            { "bufferSize", bufferSizeOption },
            { "maxConnections", maxConnectionsPerIpOption },
            { "fqdn", fqdnOption },
            { "serverType", serverTypeOption },
            { "chatServerIp", chatServerIpOption }
        };

        foreach (var option in options.Values)
        {
            rootCommand.AddOption(option);
        }

        return (rootCommand, options);
    }

    private static ServerOptions GetOptions(InvocationContext context, Dictionary<string, Option> optionsDict)
    {
        return new ServerOptions
        {
            ConfigPath = context.ParseResult.GetValueForOption((Option<string>) optionsDict["config"]),
            BindIp = context.ParseResult.GetValueForOption((Option<string>) optionsDict["bindIp"]),
            BindPort = context.ParseResult.GetValueForOption((Option<int>) optionsDict["bindPort"]),
            Backlog = context.ParseResult.GetValueForOption((Option<int>) optionsDict["backlog"]),
            BufferSize = context.ParseResult.GetValueForOption((Option<int>) optionsDict["bufferSize"]),
            MaxConnections = context.ParseResult.GetValueForOption((Option<int>) optionsDict["maxConnections"]),
            Fqdn = context.ParseResult.GetValueForOption((Option<string>) optionsDict["fqdn"]),
            ServerType = context.ParseResult.GetValueForOption((Option<string>) optionsDict["serverType"]),
            ChatServerIp = context.ParseResult.GetValueForOption((Option<string>) optionsDict["chatServerIp"])
        };
    }

    private static Server ConfigureServer(IrcType serverType, SocketServer socketServer, NtlmCredentials credentialProvider, string? chatServerIp)
    {
        var floodProtectionManager = new FloodProtectionManager();
        var securityManager = new SecurityManager();
        var dataStoreServerConfig = new DataStore("DefaultServer.json");
        var channels = new List<IChannel>();
        return serverType switch
        {
            IrcType.IRC => new Server(socketServer, securityManager, floodProtectionManager, dataStoreServerConfig, channels),
            IrcType.IRCX => new ExtendedServer(socketServer, securityManager, floodProtectionManager, dataStoreServerConfig, channels, credentialProvider),
            IrcType.ADS => ConfigureDirectoryServer(socketServer, credentialProvider, securityManager, floodProtectionManager, dataStoreServerConfig, channels, chatServerIp),
            _ => new ApolloServer(socketServer, securityManager, floodProtectionManager, dataStoreServerConfig, channels, credentialProvider)
        };
    }

    private static DirectoryServer ConfigureDirectoryServer(SocketServer socketServer, NtlmCredentials credentialProvider, SecurityManager securityManager, FloodProtectionManager floodProtectionManager, DataStore dataStoreServerConfig, List<IChannel> channels, string? chatServerIp)
    {
        var server = new DirectoryServer(socketServer, securityManager, floodProtectionManager, dataStoreServerConfig, channels, credentialProvider, chatServerIp);

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
        var defaultChannels = JsonSerializer.Deserialize<List<DefaultChannel>>(File.ReadAllText("DefaultChannels.json"));
        if (defaultChannels == null) return;

        foreach (var defaultChannel in defaultChannels)
        {
            var name = serverType == IrcType.IRC ? $"#{defaultChannel.Name}" : $"%#{defaultChannel.Name}";
            var channel = server.CreateChannel(name);

            channel.ChannelStore.Set("topic", defaultChannel.Topic);
            foreach (var keyValuePair in defaultChannel.Modes)
            {
                channel.Modes.SetModeChar(keyValuePair.Key, keyValuePair.Value);
            }

            if (channel is ExtendedChannel extendedChannel)
            {
                foreach (var keyValuePair in defaultChannel.Props)
                {
                    var prop = extendedChannel.PropCollection.GetProp(keyValuePair.Key);
                    prop?.SetValue(keyValuePair.Value);
                }
            }

            server.AddChannel(channel);
        }
    }

    private static void DisplayStartupInfo(ServerOptions options, IPAddress ip)
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║            IRC7 Server Info            ║");
        Console.WriteLine("╠════════════════════════════════════════╣");

        List<string> infoLines = new List<string>
        {
            $"║ Server Version: {Assembly.GetExecutingAssembly().GetName().Version}",
            $"║ Listening on IP: {ip}",
            $"║ Port: {options.BindPort}",
            $"║ Max Connections: {options.MaxConnections}",
            $"║ Server Type: {options.ServerType?.ToUpper()}",
            $"║ FQDN: {options.Fqdn}",
            $"║ Buffer Size: {options.BufferSize} bytes",
            $"║ Backlog Size: {options.Backlog}",
        };
        if (!string.IsNullOrEmpty(options.ChatServerIp))
        {
            infoLines.Add($"║ Chat Server Ip: {options.ChatServerIp}");
        }

        int maxLength = 0;
        foreach (var line in infoLines)
        {
            maxLength = Math.Max(maxLength, line.Length);
            maxLength = maxLength < 40 ? 40 : maxLength;
        }

        foreach (var line in infoLines)
        {
            int spacesToAdd = maxLength - line.Length;
            string formattedLine = line + new string(' ', spacesToAdd) + " ║";
            Console.WriteLine(formattedLine);
        }

        Console.WriteLine("╚════════════════════════════════════════╝");
    }

    private enum IrcType
    {
        IRC,
        IRCX,
        ACS,
        ADS
    }
}