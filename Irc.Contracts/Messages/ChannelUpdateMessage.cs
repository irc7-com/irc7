namespace Irc.Contracts.Messages;

/// <summary>
/// Payload published by the ChannelMaster BroadcastProcess to the
/// cm:channel:update pub-sub channel (doc section 4.4.4).
///
/// Each message carries a complete snapshot of all channels owned by a
/// single ChatServer. "Every update is complete and contained and does
/// not depend on any previous update." — so missing an update is harmless;
/// the next one will bring the ADS back to a consistent state.
/// </summary>
public sealed class ChannelUpdateMessage
{
    /// <summary>
    /// The ChatServer that owns all the channels in this update.
    /// </summary>
    public required string ChatServerId { get; init; }

    /// <summary>
    /// Complete list of channels currently assigned to this ChatServer.
    /// If a channel the ADS knows about is absent from this list, the ADS
    /// should remove it (the channel no longer exists on that ChatServer).
    /// </summary>
    public required ChannelUpdateEntry[] Channels { get; init; }
}

/// <summary>
/// A single channel entry within a CHANNEL-UPDATE message.
/// </summary>
public sealed class ChannelUpdateEntry
{
    public required string ChannelName { get; init; }
    public required string ChannelUid { get; init; }
    public required int MemberCount { get; init; }
}
