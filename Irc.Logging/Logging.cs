using NLog;
using NLog.Config;
using NLog.Targets;

namespace Irc.Logging;

public class Logging
{
    public static void Attach()
    {
        var config = new LoggingConfiguration();
        var logfile = new FileTarget("logfile") { FileName = "irc7d.log" };
        var logconsole = new ConsoleTarget("logconsole");

        config.AddRule(LogLevel.Fatal, LogLevel.Fatal, logfile);
        config.AddRule(LogLevel.Fatal, LogLevel.Fatal, logconsole);

        LogManager.Configuration = config;
    }
}