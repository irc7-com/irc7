namespace Irc.Interfaces;

public interface IDataRegulator
{
    bool IsIncomingThresholdExceeded();
    bool IsOutgoingThresholdExceeded();
    int GeIncomingQueueLength();
    int GetOutgoingQueueLength();
    int GetIncomingBytes();
    int GetOutgoingBytes();
    int PushIncoming(ChatMessage chatMessage);
    int PushOutgoing(string message);
    ChatMessage? PopIncoming();
    ChatMessage? PeekIncoming();
    string? PopOutgoing();
    void Purge();
}