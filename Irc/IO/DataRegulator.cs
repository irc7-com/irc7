using System.Collections.Concurrent;
using Irc.Interfaces;

namespace Irc.IO;

public class DataRegulator : IDataRegulator
{
    private readonly int _incomingByteThreshold;

    private readonly ConcurrentQueue<ChatMessage> _incomingQueue = new();
    private readonly int _outgoingByteThreshold;
    private readonly ConcurrentQueue<string> _outgoingQueue = new();
    private int _incomingBytes;
    private bool _incomingThresholdExceeded;
    private int _outgoingBytes;
    private bool _outgoingThresholdExceeded;

    public DataRegulator(int incomingByteThreshold, int outgoingByteThreshold)
    {
        _incomingByteThreshold = incomingByteThreshold;
        _outgoingByteThreshold = outgoingByteThreshold;
    }

    public int GetIncomingBytes()
    {
        return _incomingBytes;
    }

    public int GetOutgoingBytes()
    {
        return _outgoingBytes;
    }

    public bool IsIncomingThresholdExceeded()
    {
        return _incomingThresholdExceeded;
    }

    public bool IsOutgoingThresholdExceeded()
    {
        return _outgoingThresholdExceeded;
    }

    public int GeIncomingQueueLength()
    {
        return _outgoingQueue.Count;
    }

    public int GetOutgoingQueueLength()
    {
        return _outgoingQueue.Count;
    }

    public int PushIncoming(ChatMessage chatMessage)
    {
        _incomingQueue.Enqueue(chatMessage);
        _incomingBytes += chatMessage.OriginalText.Length;
        if (_incomingBytes > _incomingByteThreshold) _incomingThresholdExceeded = true;
        return _incomingBytes;
    }

    public int PushOutgoing(string message)
    {
        _outgoingQueue.Enqueue(message);
        _outgoingBytes += message.Length;
        if (_outgoingBytes > _outgoingByteThreshold) _outgoingThresholdExceeded = true;
        return _outgoingBytes;
    }

    public ChatMessage? PeekIncoming()
    {
        if (_incomingQueue.Count <= 0) return null;
        _incomingQueue.TryPeek(out var message);
        if (message == null) return null;

        return message;
    }

    public ChatMessage? PopIncoming()
    {
        _incomingQueue.TryDequeue(out var message);
        if (message == null) return null;

        _incomingBytes -= message.OriginalText.Length;
        return message;
    }

    public string? PopOutgoing()
    {
        _outgoingQueue.TryDequeue(out var message);
        if (message == null) return null;

        _outgoingBytes -= message.Length;
        return message;
    }

    public void Purge()
    {
        _incomingQueue.Clear();
        _outgoingQueue.Clear();
    }
}