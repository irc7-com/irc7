using Irc.ChannelMaster.Models;

namespace Irc.ChannelMaster.State;

public interface IChannelMasterStore
{
    Task HeartbeatChannelMasterAsync(string channelMasterId, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetActiveChannelMastersAsync(CancellationToken cancellationToken = default);
    Task<string?> GetCurrentLeaderAsync(CancellationToken cancellationToken = default);
    Task DefineLeaderAsync(string leaderId, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task<bool> TryAcquireControllerLeaseAsync(string controllerId, TimeSpan leaseTtl, CancellationToken cancellationToken = default);
    Task<bool> RenewControllerLeaseAsync(string controllerId, TimeSpan leaseTtl, CancellationToken cancellationToken = default);
    Task ReleaseControllerLeaseAsync(string controllerId, CancellationToken cancellationToken = default);

    Task HeartbeatBroadcastWorkerAsync(string workerId, int currentLoad, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BroadcastWorkerStatus>> GetActiveBroadcastWorkersAsync(CancellationToken cancellationToken = default);

    Task HeartbeatChatServerAsync(string chatServerId, string hostname, int userCount, int channelCount, ChatServerStatusType status, TimeSpan ttl, string[]? channelNames = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatServerStatus>> GetActiveChatServersAsync(CancellationToken cancellationToken = default);
    Task<ChatServerStatus?> GetChatServerAsync(string chatServerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, string>> GetChatServerAssignmentsAsync(CancellationToken cancellationToken = default);
    Task SetChatServerAssignmentAsync(string chatServerId, string broadcastWorkerId, CancellationToken cancellationToken = default);
    Task ReconcileChatServerAssignmentsAsync(CancellationToken cancellationToken = default);

    Task<bool> TryClaimChannelAsync(string channelName, string channelUid, string ownerId, DateTime createdUtc, CancellationToken cancellationToken = default);
    Task UnclaimChannelAsync(string channelName, CancellationToken cancellationToken = default);
    Task<ChannelRecord?> GetChannelRecordAsync(string channelName, CancellationToken cancellationToken = default);
    Task<ChannelRecord?> GetChannelByUidAsync(string channelUid, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, ChannelRecord>> GetAllChannelRecordsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the MemberCount on an existing ChannelRecord.
    /// Returns true if the channel was found and updated, false if not found.
    /// </summary>
    Task<bool> UpdateChannelMemberCountAsync(string channelName, int memberCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reassigns a channel to a new owner Chat Server (used during fail-over, doc 4.5.3).
    /// Returns true if the channel was found and updated, false if not found.
    /// </summary>
    Task<bool> UpdateChannelOwnerAsync(string channelName, string newOwnerServerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all channel records grouped by their OwnerServerId.
    /// Used by the BroadcastProcess to build per-ChatServer CHANNEL-UPDATE messages (doc 4.4.4).
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<ChannelRecord>>> GetChannelsByOwnerAsync(CancellationToken cancellationToken = default);
}

