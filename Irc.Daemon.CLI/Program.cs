using System.CommandLine;
using System.Text.Json;
using Irc.Security.Passport;

namespace Irc.Daemon.CLI;

internal record PassportConfig(string AppId, string Secret);

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("IRC7 Passport cookie generation utility");

        var configOption = new Option<string>(
            ["-c", "--config"],
            "Path to the Passport config JSON file (default: ./PassportConfig.json)")
        {
            ArgumentHelpName = "configfile"
        };
        configOption.SetDefaultValue("./PassportConfig.json");
        rootCommand.AddGlobalOption(configOption);

        rootCommand.AddCommand(BuildTicketCommand(configOption));
        rootCommand.AddCommand(BuildProfileCommand(configOption));
        rootCommand.AddCommand(BuildRegCookieCommand(configOption));
        rootCommand.AddCommand(BuildDecryptCommand(configOption));

        return await rootCommand.InvokeAsync(args);
    }

    private static PassportV4 LoadPassport(string configPath)
    {
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"Config file not found: '{configPath}'.");

        var json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<PassportConfig>(json,
                         new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                     ?? throw new InvalidOperationException($"Failed to deserialize config from '{configPath}'.");

        return new PassportV4(config.AppId, config.Secret);
    }

    private static Command BuildTicketCommand(Option<string> configOption)
    {
        var command = new Command("ticket", "Create a Passport ticket token");
        var puid   = new Option<string>("--puid",   "Passport Unique ID") { IsRequired = true };
        var domain = new Option<string>("--domain", "Domain")             { IsRequired = true };
        var ts     = new Option<long?> ("--ts",     "Issued-at Unix timestamp (default: UtcNow)");
        var ttl    = new Option<long>  ("--ttl",    "Time-to-live in seconds (default: 0)");
        ttl.SetDefaultValue(0L);

        command.AddOption(puid);
        command.AddOption(domain);
        command.AddOption(ts);
        command.AddOption(ttl);

        command.SetHandler((configPath, puidVal, domainVal, tsVal, ttlVal) =>
        {
            var resolvedTs = tsVal ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Console.WriteLine(LoadPassport(configPath).CreateTicket(puidVal, domainVal, resolvedTs, ttlVal));
        }, configOption, puid, domain, ts, ttl);

        return command;
    }

    private static Command BuildProfileCommand(Option<string> configOption)
    {
        var command = new Command("profile", "Create a Passport profile token");
        var pid = new Option<string>("--pid", "Profile ID")               { IsRequired = true };
        var ts  = new Option<long?> ("--ts",  "Issued-at Unix timestamp (default: UtcNow)");

        command.AddOption(pid);
        command.AddOption(ts);

        command.SetHandler((configPath, pidVal, tsVal) =>
        {
            var resolvedTs = tsVal ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Console.WriteLine(LoadPassport(configPath).CreateProfile(pidVal, resolvedTs));
        }, configOption, pid, ts);

        return command;
    }

    private static Command BuildRegCookieCommand(Option<string> configOption)
    {
        var command = new Command("regcookie", "Create a Passport registration cookie token");
        var nick = new Option<string>("--nick", "Nickname") { IsRequired = true };

        command.AddOption(nick);

        command.SetHandler((configPath, nickVal) =>
        {
            Console.WriteLine(LoadPassport(configPath).CreateRegCookie(nickVal));
        }, configOption, nick);

        return command;
    }

    private static Command BuildDecryptCommand(Option<string> configOption)
    {
        var command = new Command("decrypt", "Decrypt a Passport token and print its key-value pairs");
        var token = new Option<string>("--token", "The encrypted Passport token to decrypt") { IsRequired = true };

        command.AddOption(token);

        command.SetHandler((configPath, tokenVal) =>
        {
            var result = LoadPassport(configPath).DecryptToken(tokenVal);
            if (result.Count == 0)
            {
                Console.Error.WriteLine("Decryption failed or token is invalid.");
                return;
            }
            foreach (var kvp in result)
                Console.WriteLine($"{kvp.Key}={kvp.Value}");
        }, configOption, token);

        return command;
    }
}
