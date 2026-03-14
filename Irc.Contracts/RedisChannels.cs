namespace Irc.Contracts;

/// <summary>
/// Redis key patterns and pub-sub channel names shared between
/// Irc.ChannelMaster, Irc (ACS), and Irc.Directory (ADS).
/// 
/// All ChannelMaster-related keys use the "cm:" prefix.
/// </summary>
public static class RedisChannels
{
    // ── Chat Server Registration (direct key writes by ACS) ──────────────

    /// <summary>
    /// SET containing all registered chat server IDs.
    /// Each ACS adds its ID here during heartbeat.
    /// </summary>
    public const string ChatServerSet = "cm:chat:servers";

    /// <summary>
    /// Per-server heartbeat key pattern. Value = JSON ChatServerHeartbeat.
    /// Written by ACS with a TTL; expiry = server considered dead.
    /// Usage: string.Format(ChatServerKey, serverId)
    /// </summary>
    public const string ChatServerKeyPattern = "cm:chat:server:{0}";

    // ── Controller Commands (ADS → ChannelMaster) ────────────────────────

    /// <summary>
    /// Pub-sub channel where ADS publishes command requests
    /// (CREATE, FINDHOST) to the ChannelMaster controller.
    /// Payload = JSON ControllerRequest.
    /// </summary>
    public const string ControllerCommandChannel = "cm:cmd:controller";

    /// <summary>
    /// Temporary key pattern where ChannelMaster writes command responses.
    /// ADS polls this key after publishing a request.
    /// Usage: string.Format(ControllerReplyKeyPattern, requestId)
    /// </summary>
    public const string ControllerReplyKeyPattern = "cm:reply:{0}";

    // ── ACS Commands (ChannelMaster → ACS) ───────────────────────────────

    /// <summary>
    /// Per-server pub-sub channel where ChannelMaster publishes
    /// ASSIGN commands to a specific ACS.
    /// Usage: string.Format(AcsCommandChannelPattern, serverId)
    /// </summary>
    public const string AcsCommandChannelPattern = "cm:cmd:acs:{0}";

    /// <summary>
    /// Temporary key pattern where ACS writes ASSIGN responses.
    /// ChannelMaster polls this key after publishing a command.
    /// Usage: string.Format(AcsReplyKeyPattern, requestId)
    /// </summary>
    public const string AcsReplyKeyPattern = "cm:reply:acs:{0}";

    // ── Helpers ──────────────────────────────────────────────────────────

    public static string ChatServerKey(string serverId) =>
        string.Format(ChatServerKeyPattern, serverId);

    public static string ControllerReplyKey(string requestId) =>
        string.Format(ControllerReplyKeyPattern, requestId);

    public static string AcsCommandChannel(string serverId) =>
        string.Format(AcsCommandChannelPattern, serverId);

    public static string AcsReplyKey(string requestId) =>
        string.Format(AcsReplyKeyPattern, requestId);

    // ── Reply Key TTL ───────────────────────────────────────────────────

    /// <summary>
    /// Default TTL for temporary reply keys. Short-lived to avoid
    /// accumulating stale keys if the requester disconnects.
    /// </summary>
    public static readonly TimeSpan ReplyKeyTtl = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Default polling interval when waiting for a reply key to appear.
    /// </summary>
    public static readonly TimeSpan ReplyPollInterval = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Default timeout when waiting for a reply key.
    /// </summary>
    public static readonly TimeSpan ReplyTimeout = TimeSpan.FromSeconds(10);
}
