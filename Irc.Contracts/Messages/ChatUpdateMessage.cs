namespace Irc.Contracts.Messages;

/// <summary>
/// Payload published by an ACS (Chat Server) to the cm:chat:update pub-sub
/// channel. Carries per-channel member count deltas for channels whose
/// membership has changed since the last update (doc section 4.4.5).
///
/// A MemberCount of zero indicates the channel has been closed on the ACS.
/// The ChannelMaster should remove the channel record on receipt of a zero count.
/// </summary>
public sealed class ChatUpdateMessage
{
    public required string ChatServerId { get; init; }

    /// <summary>
    /// List of per-channel member count entries.
    /// Only channels whose count has changed since the last update are included.
    /// </summary>
    public required ChatUpdateEntry[] Entries { get; init; }
}

/// <summary>
/// A single channel's updated member count within a CHAT-UPDATE message.
/// </summary>
public sealed class ChatUpdateEntry
{
    public required string ChannelName { get; init; }
    public required int MemberCount { get; init; }
}
