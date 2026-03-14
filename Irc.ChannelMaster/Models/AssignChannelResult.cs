namespace Irc.ChannelMaster.Models;

public enum AssignChannelStatus
{
    Success,
    Busy,
    NotFound,
    NotLeader
}

public sealed class AssignChannelResult
{
    public required AssignChannelStatus Status { get; init; }
    public string? ServerId { get; init; }

    public static AssignChannelResult Success(string serverId) => new()
    {
        Status = AssignChannelStatus.Success,
        ServerId = serverId
    };

    public static AssignChannelResult Busy() => new()
    {
        Status = AssignChannelStatus.Busy
    };

    public static AssignChannelResult NotFound() => new()
    {
        Status = AssignChannelStatus.NotFound
    };

    public static AssignChannelResult NotLeader() => new()
    {
        Status = AssignChannelStatus.NotLeader
    };
}
