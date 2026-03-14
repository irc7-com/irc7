namespace Irc.ChannelMaster.Gateway;

/// <summary>
/// Outbound gateway for sending commands to Chat Servers.
/// The Controller uses this to issue ASSIGN commands (doc 4.1.2, second table).
/// Real implementations will use TCP or Redis pub-sub; tests mock this interface.
/// </summary>
public interface IChatServerGateway
{
    /// <summary>
    /// Sends an ASSIGN command to a Chat Server, telling it to host a channel.
    /// ASSIGN &lt;Channel Name&gt; &lt;Channel UID&gt; &lt;TTL&gt;
    /// </summary>
    /// <returns>true if the server accepted (SUCCESS), false if it refused (BUSY).</returns>
    Task<bool> SendAssignAsync(
        string chatServerId,
        string channelName,
        string channelUid,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);
}
