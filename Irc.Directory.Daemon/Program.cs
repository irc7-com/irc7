using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net;
using System.Reflection;
using Irc.Host;
using Irc.Interfaces;
using Irc.IO;
using Irc.Logging;
using Irc.Objects.Server;
using Irc.Security;
using NLog;

namespace Irc.Directory.Daemon;

internal class Program
{
	private static readonly Logger Log = LogManager.GetCurrentClassLogger();
	private static IServer? _server;
	private static CancellationTokenSource _cancellationTokenSource = new();

	private static async Task<int> Main(string[] args)
	{
		var trace = Array.Exists(args, a => a == "--trace" || a == "-t");
		Irc.Logging.Logging.Attach(trace: trace);
		var (rootCommand, optionsDictionary) = CreateRootCommand();

		rootCommand.SetHandler((Func<InvocationContext, Task>)(async context =>
		{
			var options = GetOptions(context, optionsDictionary);
			Log.Info($"Starting {AppDomain.CurrentDomain.FriendlyName} from {AppContext.BaseDirectory}");
			var ip = !string.IsNullOrEmpty(options.BindIp) ? IPAddress.Parse(options.BindIp) : IPAddress.Any;

			var socketServer = new SocketServer(ip, options.BindPort, options.Backlog, options.MaxConnections,
				options.MaxConnectionsPerIp, options.BufferSize);
			socketServer.OnListen += (_, __) => DisplayStartupInfo(options, ip);

			var defaultPermissions = await LoadDefaultPermissions();
			var floodProtectionManager = new FloodProtectionManager();
			var dataStoreServerConfig = new DataStore(ResolveRuntimePath("DefaultServer.json"));

			try
			{
				_server = new DirectoryServer(socketServer, (passport) => new SaslHandler(defaultPermissions, passport), floodProtectionManager, dataStoreServerConfig,
					null, options.ChatServerIp, options.RedisUrl);
			}
			catch (InvalidOperationException ex)
			{
				Log.Fatal(ex, $"Startup failed while binding {ip}:{options.BindPort}");
				Console.Error.WriteLine(ex.Message);
				context.ExitCode = 1;
				return;
			}

			_server.ServerVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
			_server.RemoteIp = options.Fqdn ?? "localhost";

			if (!string.IsNullOrEmpty(options.ServerName))
				_server.Name = options.ServerName;

			if (_server is Server baseServer)
			{
				baseServer.RecoverChannels();
				baseServer.SetupHeartbeat();
			}

			_cancellationTokenSource = new CancellationTokenSource();
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
			Console.CancelKeyPress += CurrentDomain_ProcessExit;
			await Task.Delay(-1, _cancellationTokenSource.Token).ContinueWith(_ => { });
		}));

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
		var rootCommand = new RootCommand("irc7 directory daemon")
		{
			Name = AppDomain.CurrentDomain.FriendlyName
		};

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
			new Option<int>(["-mip", "--maxConnPerIp"],
				"The maximum connections per IP that can connect (default 128)")
			{ ArgumentHelpName = "maxconnectionsperip" };
		maxConnectionsPerIpOption.SetDefaultValue(128);

		var fqdnOption = new Option<string>(["-f", "--fqdn"], "The FQDN of the machine (default localhost)")
			{ ArgumentHelpName = "fqdn" };
		fqdnOption.SetDefaultValue("localhost");

		var chatServerIpOption =
			new Option<string>(["-s", "--server"], "The Chat Server Ip and Port e.g. 127.0.0.1:6667")
				{ ArgumentHelpName = "server" };

		var redisUrlOption =
			new Option<string>(["-r", "--redis"],
				"The Redis/KeyDB connection string (optional, enables ADS/ACS load balancing)")
				{ ArgumentHelpName = "redisurl" };

		var serverNameOption =
			new Option<string>(["-n", "--name"], "The server name, overrides the Name value from DefaultServer.json")
				{ ArgumentHelpName = "servername" };

		var traceOption = new Option<bool>(["-t", "--trace"], "Enable trace logging");

		var options = new Dictionary<string, Option>
		{
			{ "bindIp", bindIpOption },
			{ "bindPort", bindPortOption },
			{ "backlog", backlogOption },
			{ "bufferSize", bufferSizeOption },
			{ "maxConnections", maxConnectionsOption },
			{ "maxConnectionsPerIp", maxConnectionsPerIpOption },
			{ "fqdn", fqdnOption },
			{ "chatServerIp", chatServerIpOption },
			{ "redisUrl", redisUrlOption },
			{ "serverName", serverNameOption },
			{ "trace", traceOption }
		};

		foreach (var option in options.Values)
			rootCommand.AddOption(option);

		return (rootCommand, options);
	}

	private static ServerOptions GetOptions(InvocationContext context, Dictionary<string, Option> optionsDict)
	{
		return new ServerOptions
		{
			BindIp = context.ParseResult.GetValueForOption((Option<string>)optionsDict["bindIp"]),
			BindPort = context.ParseResult.GetValueForOption((Option<int>)optionsDict["bindPort"]),
			Backlog = context.ParseResult.GetValueForOption((Option<int>)optionsDict["backlog"]),
			BufferSize = context.ParseResult.GetValueForOption((Option<int>)optionsDict["bufferSize"]),
			MaxConnections = context.ParseResult.GetValueForOption((Option<int>)optionsDict["maxConnections"]),
			MaxConnectionsPerIp = context.ParseResult.GetValueForOption((Option<int>)optionsDict["maxConnectionsPerIp"]),
			Fqdn = context.ParseResult.GetValueForOption((Option<string>)optionsDict["fqdn"]),
			ChatServerIp = context.ParseResult.GetValueForOption((Option<string>)optionsDict["chatServerIp"]),
			RedisUrl = context.ParseResult.GetValueForOption((Option<string>)optionsDict["redisUrl"]),
			ServerName = context.ParseResult.GetValueForOption((Option<string>)optionsDict["serverName"]),
			Trace = context.ParseResult.GetValueForOption((Option<bool>)optionsDict["trace"])
		};
	}


	private static async Task<Dictionary<string, PermissionProfile>> LoadDefaultPermissions()
	{
		var path = ResolveRuntimePath("DefaultPermissions.json");
		if (!File.Exists(path))
			return new Dictionary<string, PermissionProfile>(StringComparer.OrdinalIgnoreCase);

		var loaded = System.Text.Json.JsonSerializer.Deserialize(
			await File.ReadAllTextAsync(path),
			DirectoryDaemonJsonContext.Default.DictionaryStringPermissionProfile) ??
					 new Dictionary<string, PermissionProfile>();

		return new Dictionary<string, PermissionProfile>(loaded, StringComparer.OrdinalIgnoreCase);
	}

	private static void DisplayStartupInfo(ServerOptions options, IPAddress ip)
	{
		Console.WriteLine("Directory Server starting...");
		Console.WriteLine($"Version: {Assembly.GetExecutingAssembly().GetName().Version}");
		Console.WriteLine($"Listening IP: {ip}");
		Console.WriteLine($"Port: {options.BindPort}");
		Console.WriteLine($"Max Connections: {options.MaxConnections}");
		Console.WriteLine($"Max Connections Per IP: {options.MaxConnectionsPerIp}");
		Console.WriteLine($"FQDN: {options.Fqdn}");
		Console.WriteLine($"Buffer Size: {options.BufferSize}");
		Console.WriteLine($"Backlog Size: {options.Backlog}");
		Console.WriteLine($"Redis URL: {options.RedisUrl}");
		if (!string.IsNullOrEmpty(options.ServerName)) Console.WriteLine($"Server Name: {options.ServerName}");
		if (!string.IsNullOrEmpty(options.ChatServerIp)) Console.WriteLine($"Chat Server IP: {options.ChatServerIp}");
	}

	private static string ResolveRuntimePath(string fileName)
	{
		return Path.Combine(AppContext.BaseDirectory, fileName);
	}
}
