using Irc.ChannelMaster.Models;

namespace Irc.ChannelMaster.Controller.Commands;

public interface IControllerCommand
{
    string Name { get; }
    int MinArgs { get; }
    int MaxArgs { get; }

    Task<ControllerCommandResponse> ExecuteAsync(
        ControllerProcess controller,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}
