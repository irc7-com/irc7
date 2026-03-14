using Irc.ChannelMaster.Models;

namespace Irc.ChannelMaster.Controller.Commands;

public sealed class CreateCommand : ControllerCommandBase
{
    public CreateCommand() : base("CREATE", minArgs: 1, maxArgs: 1)
    {
    }

    protected override async Task<ControllerCommandResponse> ExecuteCoreAsync(
        ControllerProcess controller,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var result = await controller.CreateChannelAsync(arguments[0], cancellationToken);

        return result.Status switch
        {
            CreateChannelStatus.Success => ControllerCommandResponse.Success(result.ServerId!, result.ChannelUid!),
            CreateChannelStatus.Busy => ControllerCommandResponse.Busy(),
            CreateChannelStatus.NameConflict => ControllerCommandResponse.NameConflict(),
            CreateChannelStatus.NotLeader => ControllerCommandResponse.Error("NOT", "LEADER"),
            _ => ControllerCommandResponse.Error("UNKNOWN")
        };
    }
}
