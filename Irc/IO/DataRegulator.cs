namespace Irc;

public class DataRegulator : IDataRegulator
{
    private readonly int _incomingByteThreshold;

    private readonly Queue<Message> _incomingQueue = new();
    private readonly int _outgoingByteThreshold;
    private readonly Queue<string> _outgoingQueue = new();
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

    public int PushIncoming(Message message)
    {
        _incomingQueue.Enqueue(message);
        _incomingBytes += message.OriginalText.Length;
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

    public Message? PeekIncoming()
    {
        if (_incomingQueue.Count <= 0) return null; 
        return _incomingQueue.Peek();
    }

    public Message PopIncoming()
    {
        var message = _incomingQueue.Dequeue();
        _incomingBytes -= message.OriginalText.Length;
        return message;
    }

    public string PopOutgoing()
    {
        var message = _outgoingQueue.Dequeue();
        _outgoingBytes -= message.Length;
        return message;
    }

    public void Purge()
    {
        _incomingQueue.Clear();
        _outgoingQueue.Clear();
    }
}