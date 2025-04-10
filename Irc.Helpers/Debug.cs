using NLog;

namespace Irc.Helpers;

public static class Debug
{
    public static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static bool Enabled { get; private set; }

    public static bool Verbose { get; private set; }

    public static void Out(string s)
    {
        if (Verbose) Log.Debug(s);
    }

    public static void Enable()
    {
        Enabled = true;
    }

    public static void Disable()
    {
        Enabled = false;
    }

    public static void EnableVerbose()
    {
        Verbose = true;
    }

    public static void DisableVerbose()
    {
        Verbose = false;
    }
}