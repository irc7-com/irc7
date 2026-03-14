namespace Irc.Contracts.Messages;

/// <summary>
/// A command response from the ChannelMaster controller to an ADS.
/// Written as JSON to the temporary key cm:reply:{RequestId}.
/// </summary>
public sealed class ControllerResponse
{
    /// <summary>
    /// Correlation ID matching the original request.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Result status: SUCCESS, BUSY, NAME_CONFLICT, NOT_FOUND, ERROR.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Response values (e.g., serverId, channelUid, hostname).
    /// Empty array for error/busy responses.
    /// </summary>
    public required string[] Values { get; init; }

    // ── Well-known status codes ──────────────────────────────────────────

    public const string StatusSuccess = "SUCCESS";
    public const string StatusBusy = "BUSY";
    public const string StatusNameConflict = "NAME_CONFLICT";
    public const string StatusNotFound = "NOT_FOUND";
    public const string StatusError = "ERROR";

    /// <summary>
    /// For a successful CREATE response, the assigned server ID.
    /// </summary>
    public string? ServerId => Status == StatusSuccess && Values.Length > 0 ? Values[0] : null;

    /// <summary>
    /// For a successful CREATE response, the channel UID.
    /// </summary>
    public string? ChannelUid => Status == StatusSuccess && Values.Length > 1 ? Values[1] : null;

    /// <summary>
    /// For a successful FINDHOST response, the server hostname.
    /// </summary>
    public string? Hostname => Status == StatusSuccess && Values.Length > 0 ? Values[0] : null;
}
