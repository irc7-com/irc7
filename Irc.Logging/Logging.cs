using NLog;
using NLog.LayoutRenderers;
using NLog.Targets;

namespace Irc.Logging;

[Target("ConsoleWriteLine")]
public sealed class ConsoleWriteLineTarget : TargetWithLayout
{
    protected override void Write(LogEventInfo logEvent)
    {
        Console.WriteLine(RenderLogEvent(Layout, logEvent));
    }
}

public class Logging
{
    private const string Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}${onexception:inner=${newline}${exception:format=tostring}}";

    public static bool TraceEnabled { get; private set; }

    public static void Attach(bool trace = false)
    {
        TraceEnabled = trace;
        var logFilePath = Path.Combine(AppContext.BaseDirectory, "irc7d.log");

        var minLevel = trace ? LogLevel.Trace : LogLevel.Info;

        LogManager.Setup()
            .SetupExtensions(e =>
            {
                e.RegisterLayoutRenderer<LongDateLayoutRenderer>("longdate");
                e.RegisterLayoutRenderer<LevelLayoutRenderer>("level");
                e.RegisterLayoutRenderer<LoggerNameLayoutRenderer>("logger");
                e.RegisterLayoutRenderer<MessageLayoutRenderer>("message");
                e.RegisterLayoutRenderer<ExceptionLayoutRenderer>("exception");
                e.RegisterLayoutRenderer<NewLineLayoutRenderer>("newline");
                e.RegisterTarget<ConsoleWriteLineTarget>("ConsoleWriteLine");
                e.RegisterTarget<FileTarget>("file");
            })
            .LoadConfiguration(c =>
            {
                c.ForLogger().FilterMinLevel(minLevel)
                    .WriteTo(new ConsoleWriteLineTarget { Layout = Layout });

                c.ForLogger().FilterMinLevel(minLevel)
                    .WriteToFile(fileName: logFilePath, layout: Layout);
            });

        LogManager.ReconfigExistingLoggers();
    }
}

