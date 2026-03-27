using Irc.ChannelMaster.Models;
using Irc.ChannelMaster.State;
using Irc.Contracts.Messages;

namespace Irc.ChannelMaster.Broadcast;

public sealed class BroadcastProcess
{
    private readonly IChannelMasterStore _store;

    public BroadcastProcess(IChannelMasterStore store, string workerId)
    {
        _store = store;
        WorkerId = workerId;
    }

    public string WorkerId { get; }
    public TimeSpan WorkerTtl { get; set; } = TimeSpan.FromSeconds(15);

    public IChannelMasterStore Store => _store;

    public async Task<BroadcastAssignmentSnapshot> RunOnceAsync(int currentLoad, CancellationToken cancellationToken = default)
    {
        await _store.HeartbeatBroadcastWorkerAsync(WorkerId, currentLoad, WorkerTtl, cancellationToken);

        var assignments = await _store.GetChatServerAssignmentsAsync(cancellationToken);
        var assignedChatServers = assignments
            .Where(kvp => kvp.Value.Equals(WorkerId, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new BroadcastAssignmentSnapshot
        {
            WorkerId = WorkerId,
            ReportedLoad = currentLoad,
            ChatServerIds = assignedChatServers
        };
    }

    /// <summary>
    /// Processes a CHAT-UPDATE message from an ACS (doc section 4.4.5).
    /// Updates per-channel member counts in the store.
    /// A member count of zero indicates the channel has been closed on the ACS;
    /// the record is removed from the store.
    /// </summary>
    public async Task HandleChatUpdateAsync(ChatUpdateMessage message, CancellationToken cancellationToken = default)
    {
        if (message.Entries == null || message.Entries.Length == 0) return;

        foreach (var entry in message.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.ChannelName)) continue;

            if (entry.MemberCount <= 0)
            {
                // Doc: "A member-count of zero indicates that the Channel has Closed.
                // The Chat Server should send a zero-count message to the Channel Master
                // prior to deleting the Channel."
                await _store.UnclaimChannelAsync(entry.ChannelName, cancellationToken);
                Console.WriteLine(
                    $"[Broadcast] Channel {entry.ChannelName} closed by {message.ChatServerId} (member count = 0)");
            }
            else
            {
                var updated = await _store.UpdateChannelMemberCountAsync(
                    entry.ChannelName, entry.MemberCount, cancellationToken);

                if (!updated)
                {
                    Console.WriteLine(
                        $"[Broadcast] Ignoring CHAT-UPDATE for unknown channel {entry.ChannelName} from {message.ChatServerId}");
                }
            }
        }
    }

    /// <summary>
    /// Builds CHANNEL-UPDATE messages for all ChatServers assigned to this
    /// broadcast worker (doc section 4.4.4). Each message is a complete
    /// snapshot of channels owned by one ChatServer.
    ///
    /// "Every update is complete and contained and does not depend on any
    /// previous update."
    ///
    /// Returns one ChannelUpdateMessage per ChatServer. ChatServers with
    /// no channels still produce a message with an empty Channels array,
    /// so the ADS can clean up stale data.
    /// </summary>
    public async Task<IReadOnlyList<ChannelUpdateMessage>> BuildChannelUpdatesAsync(CancellationToken cancellationToken = default)
    {
        // Get the ChatServers assigned to this broadcast worker
        var assignments = await _store.GetChatServerAssignmentsAsync(cancellationToken);
        var assignedChatServers = assignments
            .Where(kvp => kvp.Value.Equals(WorkerId, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (assignedChatServers.Count == 0) return [];

        // Get all channels grouped by owner
        var channelsByOwner = await _store.GetChannelsByOwnerAsync(cancellationToken);

        var messages = new List<ChannelUpdateMessage>();
        foreach (var chatServerId in assignedChatServers.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))
        {
            ChannelUpdateEntry[] entries;
            if (channelsByOwner.TryGetValue(chatServerId, out var channels))
            {
                entries = channels.Select(r => new ChannelUpdateEntry
                {
                    ChannelName = r.ChannelName,
                    ChannelUid = r.ChannelUid,
                    MemberCount = r.MemberCount
                }).ToArray();
            }
            else
            {
                // No channels for this ChatServer — send empty snapshot
                // so ADS removes any stale data it had for this server
                entries = [];
            }

            messages.Add(new ChannelUpdateMessage
            {
                ChatServerId = chatServerId,
                Channels = entries
            });
        }

        return messages;
    }
}
