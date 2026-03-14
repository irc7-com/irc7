namespace Irc.ChannelMaster.Models;

public sealed class BroadcastAssignmentSnapshot
{
    public required string WorkerId { get; init; }
    public required int ReportedLoad { get; init; }
    public IReadOnlyList<string> ChatServerIds { get; init; } = Array.Empty<string>();
}
