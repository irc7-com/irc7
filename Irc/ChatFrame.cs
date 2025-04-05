using Irc.Interfaces;
using Irc.Objects;
using Irc.Objects.Server;

namespace Irc;

public class ChatFrame : IChatFrame
{
    public long SequenceId { get; set; }
    public required Message Message { init; get; }
    public required IServer Server { init; get; }
    public required IUser User { init; get; }
}