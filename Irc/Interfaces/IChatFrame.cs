namespace Irc.Interfaces;

public interface IChatFrame
{
    long SequenceId { get; set; }
    ChatMessage ChatMessage { get; }
    IServer Server { get; }
    IUser User { get; }
}