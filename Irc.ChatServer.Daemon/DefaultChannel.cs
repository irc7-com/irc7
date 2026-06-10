using Irc.Helpers;
using Irc.Objects.Channel;

namespace Irc7d;

public class DefaultChannel
{
    public string Name { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Locale { get; set; } = string.Empty;
    public int Language { get; set; } = 1;
    public Dictionary<char, int> Modes { get; set; } = new();
    public Dictionary<string, string> Props { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryChannel ToInMemoryChannel()
    {
        var channelName = $"%#{Name.ToEscape()}";
        var channelTopic = $"%{Topic.ToEscape()}";

        var enabledModes = string.Concat(Modes
            .Where(kvp => kvp.Value > 0)
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => kvp.Key));

        var userLimit = Modes.TryGetValue('l', out var configuredUserLimit) && configuredUserLimit > 0
            ? configuredUserLimit
            : 50;

        Props.TryGetValue("OWNERKEY", out var ownerKey);
        Props.TryGetValue("HOSTKEY", out var hostKey);

        return new InMemoryChannel
        {
            ChannelName = channelName,
            ChannelTopic = channelTopic,
            Category = Category,
            Modes = enabledModes,
            UserLimit = userLimit,
            Locale = Locale,
            Language = Language > 0 ? Language : 1,
            OwnerKey = ownerKey ?? string.Empty,
            HostKey = hostKey ?? string.Empty
        };
    }
}