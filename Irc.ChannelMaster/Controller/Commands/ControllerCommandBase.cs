using Irc.ChannelMaster.Models;

namespace Irc.ChannelMaster.Controller.Commands;

public abstract class ControllerCommandBase : IControllerCommand
{
    protected ControllerCommandBase(string name, int minArgs, int maxArgs = -1)
    {
        Name = name;
        MinArgs = minArgs;
        MaxArgs = maxArgs;
    }

    public string Name { get; }
    public int MinArgs { get; }
    public int MaxArgs { get; }

    public Task<ControllerCommandResponse> ExecuteAsync(
        ControllerProcess controller,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        if (arguments.Count < MinArgs ||
            (MaxArgs >= 0 && arguments.Count > MaxArgs))
        {
            return Task.FromResult(
                ControllerCommandResponse.Error(Name, "REQUIRES", $"{MinArgs}", "ARGUMENT"));
        }

        // Reject blank arguments for commands that require at least one
        if (MinArgs > 0 && arguments.Count > 0 && string.IsNullOrWhiteSpace(arguments[0]))
        {
            return Task.FromResult(
                ControllerCommandResponse.Error(Name, "REQUIRES", $"{MinArgs}", "ARGUMENT"));
        }

        return ExecuteCoreAsync(controller, arguments, cancellationToken);
    }

    protected abstract Task<ControllerCommandResponse> ExecuteCoreAsync(
        ControllerProcess controller,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken);
}
