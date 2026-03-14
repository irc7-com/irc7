using Irc.ChannelMaster.Models;

namespace Irc.ChannelMaster.Controller.Commands;

/// <summary>
/// FINDHOST command (doc 4.1.3).
/// A client asks which Chat Server hosts a given channel.
/// FINDHOST &lt;channel-name&gt; → returns &lt;hostname&gt;.
/// </summary>
public sealed class FindHostCommand : ControllerCommandBase
{
    public FindHostCommand() : base("FINDHOST", minArgs: 1, maxArgs: 1) { }

    protected override async Task<ControllerCommandResponse> ExecuteCoreAsync(
        ControllerProcess controller,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var hostname = await controller.FindHostAsync(arguments[0], cancellationToken);

        return hostname != null
            ? ControllerCommandResponse.Success(hostname)
            : ControllerCommandResponse.NotFound();
    }
}
