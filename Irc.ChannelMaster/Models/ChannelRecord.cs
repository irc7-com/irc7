namespace Irc.ChannelMaster.Models;

public sealed class ChannelRecord
{
    public required string ChannelUid { get; init; }
    public required string ChannelName { get; init; }
    public required string OwnerServerId { get; set; }
    public required DateTime CreatedUtc { get; init; }

    /// <summary>
    /// Current number of members in the channel, updated via CHAT-UPDATE
    /// messages from the owning ACS (doc section 4.4.5).
    /// Defaults to 0 (unknown) until the first update is received.
    /// </summary>
    public int MemberCount { get; set; }
}
