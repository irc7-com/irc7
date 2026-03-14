namespace Irc.Contracts.Messages;

/// <summary>
/// An ASSIGN command from the ChannelMaster to an ACS (Chat Server).
/// Published as JSON to the cm:cmd:acs:{serverId} pub-sub channel.
/// The ACS writes its response to cm:reply:acs:{RequestId}.
/// </summary>
public sealed class AssignRequest
{
    /// <summary>
    /// Unique correlation ID for this request.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// The channel name to host.
    /// </summary>
    public required string ChannelName { get; init; }

    /// <summary>
    /// The globally unique channel UID assigned by the ChannelMaster.
    /// </summary>
    public required string ChannelUid { get; init; }

    /// <summary>
    /// How long the ACS should wait for the first user to join
    /// before releasing the channel. Seconds.
    /// </summary>
    public required int TtlSeconds { get; init; }
}
