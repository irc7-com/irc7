using Irc.ChannelMaster.Models;

namespace Irc.ChannelMaster.Controller.Commands;

/// <summary>
/// Inbound ASSIGN command (doc 4.1.2, first table).
/// A Channel Server asks the Controller to assign a registered channel to a Chat Server.
/// ASSIGN &lt;Channel UID&gt; → returns &lt;Server UID&gt;.
/// </summary>
public sealed class AssignCommand : ControllerCommandBase
{
    public AssignCommand() : base("ASSIGN", minArgs: 1, maxArgs: 1) { }

    protected override async Task<ControllerCommandResponse> ExecuteCoreAsync(
        ControllerProcess controller,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var result = await controller.AssignChannelAsync(arguments[0], cancellationToken);

        return result.Status switch
        {
            AssignChannelStatus.Success   => ControllerCommandResponse.Success(result.ServerId!),
            AssignChannelStatus.Busy      => ControllerCommandResponse.Busy(),
            AssignChannelStatus.NotFound  => ControllerCommandResponse.NotFound(),
            AssignChannelStatus.NotLeader => ControllerCommandResponse.Error("NOT", "LEADER"),
            _                            => ControllerCommandResponse.Error("UNKNOWN")
        };
    }
}
