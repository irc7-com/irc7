namespace Irc.Contracts.Messages;

/// <summary>
/// Payload written by an ACS (Chat Server) to its heartbeat key
/// at cm:chat:server:{serverId}. The ChannelMaster reads this
/// to track server health and compute load.
/// 
/// Must stay in sync with Irc.ChannelMaster.Models.ChatServerStatus
/// serialization format.
/// </summary>
public sealed class ChatServerHeartbeat
{
    public required string ChatServerId { get; init; }
    public required string Hostname { get; init; }
    public required int UserCount { get; init; }
    public required int ChannelCount { get; init; }
    public required string Status { get; init; }
    public required DateTime LastSeenUtc { get; init; }

    /// <summary>
    /// Names of channels currently hosted on this Chat Server.
    /// Used by the ChannelMaster to reconcile channels that exist
    /// on the ACS but are not yet tracked (e.g., default channels
    /// created at startup).
    /// </summary>
    public string[] ChannelNames { get; init; } = [];

    /// <summary>Status value for an active server accepting new channels/users.</summary>
    public const string StatusActive = "Active";

    /// <summary>Status value for a standby server not accepting new work.</summary>
    public const string StatusStandby = "Standby";
}
