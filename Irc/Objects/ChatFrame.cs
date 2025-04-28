using Irc.Interfaces;

namespace Irc.Objects;

public class ChatFrame : IChatFrame
{
    public long SequenceId { get; set; }
    public required IChatMessage ChatMessage { init; get; }
    public required IServer Server { init; get; }
    public required IUser User { init; get; }
}