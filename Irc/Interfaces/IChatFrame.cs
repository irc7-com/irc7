namespace Irc.Interfaces;

public interface IChatFrame
{
    long SequenceId { get; set; }
    IChatMessage ChatMessage { get; }
    IServer Server { get; }
    IUser User { get; }
}