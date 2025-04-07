using Irc.Interfaces;

namespace Irc;

public class ChatFrame : IChatFrame
{
    public long SequenceId { get; set; }
    public required ChatMessage ChatMessage { init; get; }
    public required IServer Server { init; get; }
    public required IUser User { init; get; }
}