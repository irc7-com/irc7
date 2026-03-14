namespace Irc.ChannelMaster.Gateway;

/// <summary>
/// Placeholder gateway that accepts all ASSIGN requests.
/// Will be replaced by a TCP or Redis pub-sub implementation.
/// </summary>
public sealed class NoOpChatServerGateway : IChatServerGateway
{
    public Task<bool> SendAssignAsync(
        string chatServerId,
        string channelName,
        string channelUid,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
