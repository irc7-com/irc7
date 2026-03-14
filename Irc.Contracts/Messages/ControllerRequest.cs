namespace Irc.Contracts.Messages;

/// <summary>
/// A command request from an ADS (Directory Server) to the ChannelMaster controller.
/// Published as JSON to the cm:cmd:controller pub-sub channel.
/// The response is written to cm:reply:{RequestId}.
/// </summary>
public sealed class ControllerRequest
{
    /// <summary>
    /// Unique correlation ID for this request. The response will be
    /// written to a Redis key derived from this ID.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Command name: CREATE, FINDHOST, ASSIGN, etc.
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Command arguments (e.g., channel name for CREATE/FINDHOST,
    /// channel UID for ASSIGN).
    /// </summary>
    public required string[] Arguments { get; init; }

    /// <summary>
    /// ID of the requesting server (for logging/diagnostics).
    /// </summary>
    public string? RequesterId { get; init; }
}
