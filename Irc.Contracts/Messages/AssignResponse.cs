namespace Irc.Contracts.Messages;

/// <summary>
/// Response from an ACS (Chat Server) to an ASSIGN command.
/// Written as JSON to the temporary key cm:reply:acs:{RequestId}.
/// </summary>
public sealed class AssignResponse
{
    /// <summary>
    /// Correlation ID matching the original request.
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Whether the ACS accepted the assignment.
    /// true = SUCCESS (channel created), false = BUSY (server refused).
    /// </summary>
    public required bool Accepted { get; init; }
}
