namespace Irc;

public interface IDataRegulator
{
    bool IsIncomingThresholdExceeded();
    bool IsOutgoingThresholdExceeded();
    int GeIncomingQueueLength();
    int GetOutgoingQueueLength();
    int GetIncomingBytes();
    int GetOutgoingBytes();
    int PushIncoming(Message message);
    int PushOutgoing(string message);
    Message PopIncoming();
    Message? PeekIncoming();
    string PopOutgoing();
    void Purge();
}