using Irc.ChannelMaster.Broadcast;
using Irc.ChannelMaster.Controller;
using Irc.ChannelMaster.State;
using Irc.Logging;
using NLog;

namespace Irc.ChannelMaster;

public static class Program
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task<int> Main(string[] args)
    {
        Irc.Logging.Logging.Attach();

        var options = CliOptions.Parse(args);
        if (options.ShowHelp)
        {
            CliOptions.PrintHelp();
            return 0;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        var instanceId = string.IsNullOrWhiteSpace(options.InstanceId)
            ? $"cm-{Environment.MachineName}-{Environment.ProcessId}"
            : options.InstanceId;

        var store = CreateStore(options);
        var controller = new ControllerProcess(store, $"{instanceId}:controller");
        var broadcast = new BroadcastProcess(store, $"{instanceId}:broadcast");

        try
        {
            Log.Info($"[ChannelMaster] mode={options.Mode}, store={options.Store}");

            if (options.RunOnce)
            {
                await RunModeOnceAsync(options, controller, broadcast, cancellation.Token);
                return 0;
            }

            while (!cancellation.IsCancellationRequested)
            {
                await RunModeOnceAsync(options, controller, broadcast, cancellation.Token);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellation.Token);
            }

            return 0;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Expected during Ctrl+C shutdown.
            Log.Info("[ChannelMaster] Cancellation requested. Shutting down.");
            return 0;
        }
        finally
        {
            await controller.StopAsync(CancellationToken.None);
            if (store is IDisposable disposableStore) disposableStore.Dispose();
        }
    }

    private static IChannelMasterStore CreateStore(CliOptions options)
    {
        return options.Store switch
        {
            StoreMode.Memory => new InMemoryChannelMasterStore(),
            StoreMode.Redis when !string.IsNullOrWhiteSpace(options.RedisConnectionString) =>
                new RedisChannelMasterStore(options.RedisConnectionString),
            StoreMode.Redis => throw new InvalidOperationException("--redis must be provided when --store redis is selected."),
            _ => throw new InvalidOperationException("Unsupported store mode.")
        };
    }

    private static async Task RunModeOnceAsync(
        CliOptions options,
        ControllerProcess controller,
        BroadcastProcess broadcast,
        CancellationToken cancellationToken)
    {
        if (options.Mode is ProcessMode.Controller or ProcessMode.Both)
        {
            var isLeader = await controller.RunOnceAsync(cancellationToken);
            Log.Info($"[Controller] leader={isLeader}");
        }

        if (options.Mode is ProcessMode.Broadcast or ProcessMode.Both)
        {
            var snapshot = await broadcast.RunOnceAsync(options.WorkerLoad, cancellationToken);
            Log.Info($"[Broadcast] worker={snapshot.WorkerId}, assigned_chat_servers={snapshot.ChatServerIds.Count}");
        }
    }

    private sealed class CliOptions
    {
        public ProcessMode Mode { get; init; } = ProcessMode.Both;
        public StoreMode Store { get; init; } = StoreMode.Memory;
        public string? RedisConnectionString { get; init; }
        public string? InstanceId { get; init; }
        public int WorkerLoad { get; init; }
        public bool RunOnce { get; init; }
        public bool ShowHelp { get; init; }

        public static CliOptions Parse(string[] args)
        {
            var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (!arg.StartsWith("--", StringComparison.Ordinal)) continue;

                var key = arg[2..];
                var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
                parsed[key] = hasValue ? args[++i] : "true";
            }

            var mode = ParseMode(parsed.TryGetValue("mode", out var modeRaw) ? modeRaw : null);
            var store = ParseStore(parsed.TryGetValue("store", out var storeRaw) ? storeRaw : null);
            var workerLoad = parsed.TryGetValue("worker-load", out var workerRaw) && int.TryParse(workerRaw, out var load)
                ? load
                : 0;

            return new CliOptions
            {
                Mode = mode,
                Store = store,
                RedisConnectionString = parsed.TryGetValue("redis", out var redis) ? redis : null,
                InstanceId = parsed.TryGetValue("id", out var id) ? id : null,
                WorkerLoad = Math.Max(0, workerLoad),
                RunOnce = parsed.ContainsKey("once"),
                ShowHelp = parsed.ContainsKey("help") || parsed.ContainsKey("h")
            };
        }

        public static void PrintHelp()
        {
            Log.Info("ChannelMaster options:");
            Log.Info("  --mode controller, broadcast, both Process role to execute (default: both)");
            Log.Info("  --store memory, redis              State backend (default: memory)");
            Log.Info("  --redis <connection-string>        Redis connection string (required for --store redis)");
            Log.Info("  --id <instance-id>                 Instance identifier");
            Log.Info("  --worker-load <int>                Current worker load for broadcast heartbeat");
            Log.Info("  --once                             Run one control iteration and exit");
            Log.Info("  --help                             Show this help");
        }

        private static ProcessMode ParseMode(string? mode)
        {
            return mode?.ToLowerInvariant() switch
            {
                "controller" => ProcessMode.Controller,
                "broadcast" => ProcessMode.Broadcast,
                _ => ProcessMode.Both
            };
        }

        private static StoreMode ParseStore(string? store)
        {
            return store?.ToLowerInvariant() switch
            {
                "redis" => StoreMode.Redis,
                _ => StoreMode.Memory
            };
        }
    }

    private enum ProcessMode
    {
        Controller,
        Broadcast,
        Both
    }

    private enum StoreMode
    {
        Memory,
        Redis
    }
}

