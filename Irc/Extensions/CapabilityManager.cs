using Irc.Constants;

namespace Irc.Extensions;

/// <summary>
/// Manages the set of IRCv3 capabilities supported by this server.
/// </summary>
public static class CapabilityManager
{
    private static readonly string[] SupportedCapabilities =
    [
        Resources.CapMultiPrefix
    ];

    private static readonly HashSet<string> SupportedSet =
        new(SupportedCapabilities, StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns the list of capabilities advertised in CAP LS.</summary>
    public static IReadOnlyList<string> GetSupportedCapabilities() => SupportedCapabilities;

    /// <summary>Returns true when the capability name is supported by this server.</summary>
    public static bool IsSupported(string capability) => SupportedSet.Contains(capability);
}
