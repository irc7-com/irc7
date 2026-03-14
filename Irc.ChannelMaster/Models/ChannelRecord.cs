namespace Irc.ChannelMaster.Models;

public sealed class ChannelRecord
{
    public required string ChannelUid { get; init; }
    public required string ChannelName { get; init; }
    public required string OwnerServerId { get; init; }
    public required DateTime CreatedUtc { get; init; }
}
