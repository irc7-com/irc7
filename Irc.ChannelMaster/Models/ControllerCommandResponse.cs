namespace Irc.ChannelMaster.Models;

public sealed class ControllerCommandResponse
{
    public required string Status { get; init; }
    public IReadOnlyList<string> Arguments { get; init; } = Array.Empty<string>();

    public string ToProtocolString() => Arguments.Count == 0
        ? Status
        : $"{Status} {string.Join(' ', Arguments)}";

    public static ControllerCommandResponse Success(params string[] arguments) => new()
    {
        Status = "SUCCESS",
        Arguments = arguments
    };

    public static ControllerCommandResponse Busy() => new()
    {
        Status = "BUSY"
    };

    public static ControllerCommandResponse NameConflict() => new()
    {
        Status = "NAME CONFLICT"
    };

    public static ControllerCommandResponse Error(params string[] arguments) => new()
    {
        Status = "ERROR",
        Arguments = arguments
    };
}
