namespace Irc.ChannelMaster.Models;

public enum CreateChannelStatus
{
    Success,
    Busy,
    NameConflict,
    NotLeader
}

public sealed class CreateChannelResult
{
    public required CreateChannelStatus Status { get; init; }
    public string? ServerId { get; init; }
    public string? ChannelUid { get; init; }

    public static CreateChannelResult Success(string serverId, string channelUid) => new()
    {
        Status = CreateChannelStatus.Success,
        ServerId = serverId,
        ChannelUid = channelUid
    };

    public static CreateChannelResult Busy() => new()
    {
        Status = CreateChannelStatus.Busy
    };

    public static CreateChannelResult NameConflict() => new()
    {
        Status = CreateChannelStatus.NameConflict
    };

    public static CreateChannelResult NotLeader() => new()
    {
        Status = CreateChannelStatus.NotLeader
    };
}
