using System.Text.Json.Serialization;

namespace Irc.ChannelMaster.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChatServerStatusType
{
    Active,
    Standby
}

public sealed class ChatServerStatus
{
    /// <summary>
    /// Default occupancy factor from Apollo architecture doc Section 2.2.
    /// Load = UserCount + ChannelCount * ChannelOccupancyFactor
    /// </summary>
    public const int DefaultChannelOccupancyFactor = 5;

    public required string ChatServerId { get; init; }
    public required string Hostname { get; init; }
    public required int UserCount { get; init; }
    public required int ChannelCount { get; init; }
    public required ChatServerStatusType Status { get; init; }
    public required DateTime LastSeenUtc { get; init; }

    /// <summary>
    /// Channel names currently hosted on this Chat Server, as reported
    /// by the ACS heartbeat. Used for reconciliation — channels that
    /// appear here but are not in the ChannelMaster store (e.g., default
    /// channels created at ACS startup) will be claimed automatically.
    /// </summary>
    public string[] ChannelNames { get; init; } = [];

    /// <summary>
    /// Computed load using the Apollo formula: Users + Channels * CHANNEL_OCCUPANCY_FACTOR.
    /// </summary>
    public int CurrentLoad => UserCount + ChannelCount * DefaultChannelOccupancyFactor;
}
