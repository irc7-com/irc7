using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Irc.Helpers;
using Irc.Interfaces;

namespace Irc7d;

public class SocketConnection : IConnection
{
    private static readonly ConcurrentBag<SocketAsyncEventArgs> ArgsPool = new();
    private static readonly ArrayPool<byte> BufferPool = ArrayPool<byte>.Shared;
    private const int BufferSize = 512; // 1KB per buffer
    private readonly string _fullAddress = string.Empty;
    private readonly Socket _socket;
    private string _address = string.Empty;
    private string _hostname = string.Empty;
    private long _idHigh; // Upper 64 bits for ip address
    private long _idLow;  // Lower 64 bits for ip address
    private IPAddress _ipAddress = new(0);

    public SocketConnection(Socket socket)
    {
        _socket = socket;

        if (_socket.RemoteEndPoint != null)
        {
            var remoteEndPoint = (IPEndPoint)_socket.RemoteEndPoint;
            _fullAddress = _socket.RemoteEndPoint != null
                ? remoteEndPoint.ToString()
                : string.Empty;
            _assignIPAddress(remoteEndPoint.Address);
        }
    }
    
    public static void InitializeSocketAsyncEventArgsPool(int capacity)
    {
        for (int i = 0; i < capacity; i++)
        {
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(GetBuffer(), 0, BufferSize);
            ArgsPool.Add(args);
        }
    }

    public EventHandler<string>? OnSend { get; set; }
    public EventHandler<string>? OnReceive { get; set; }
    public EventHandler<long>? OnConnect { get; set; }
    public EventHandler<long>? OnDisconnect { get; set; }
    public EventHandler<Exception>? OnError { get; set; }

    public string GetIp()
    {
        return _address;
    }

    public string GetIpAndPort()
    {
        return _fullAddress;
    }

    public string GetHostname()
    {
        return _hostname;
    }

    public long GetId()
    {
        // Use XOR to combine the high and low parts into a single long
        // This maintains uniqueness for most practical scenarios
        return _idHigh ^ _idLow;
    }

    public void Send(string message)
    {
        var sendAsync = new SocketAsyncEventArgs();
        sendAsync.SetBuffer(message.ToByteArray());
        sendAsync.Completed += (_, _) => OnSend?.Invoke(this, message);

        if (!_socket.Connected) OnDisconnect?.Invoke(this, GetId());

        if (_socket.Connected)
            if (!_socket.SendAsync(sendAsync)) // Report data is sent
                OnSend?.Invoke(this, message.Substring(sendAsync.Offset, sendAsync.BytesTransferred));
    }

    public void Disconnect(string message = "")
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            Send(message);
            _socket.Close();
        }
        else
        {
            _socket.Close();
        }

        if (!_socket.Connected) OnDisconnect?.Invoke(this, GetId());
    }

    public void Accept()
    {
        var recvAsync = GetSocketAsyncEventArgs();
        recvAsync.UserToken = GetId();
        //recvAsync.SetBuffer(new byte[_socket.SendBufferSize]);
        recvAsync.Completed += OnRecvAsyncOnCompleted;
        // If Sync receive from connect then process data
        if (!_socket.ReceiveAsync(recvAsync)) ReceiveData(recvAsync);
    }

    private void OnRecvAsyncOnCompleted(object? _, SocketAsyncEventArgs args)
    {
        ReceiveData(args);
    }

    private static SocketAsyncEventArgs GetSocketAsyncEventArgs()
    {
        // Try to get an existing SocketAsyncEventArgs from the pool
        if (ArgsPool.TryTake(out var args))
        {
            // If we successfully retrieved an args object from the pool
            if (args.Buffer == null)
            {
                // If buffer is null, set a new buffer
                args.SetBuffer(GetBuffer(), 0, BufferSize);
            }
        
            return args;
        }
    
        // If pool is empty, create a new SocketAsyncEventArgs
        var newArgs = new SocketAsyncEventArgs();
        newArgs.SetBuffer(GetBuffer(), 0, BufferSize);
        return newArgs;
    }

    private static void ReturnSocketAsyncEventArgs(SocketAsyncEventArgs args)
    {
        if (args.Buffer != null)
        {
            ReturnBuffer(args.Buffer);
        }
        args.SetBuffer(null, 0, 0);
        args.Completed -= null;
        ArgsPool.Add(args);
    }

    private static byte[] GetBuffer() => BufferPool.Rent(BufferSize);

    private static void ReturnBuffer(byte[] buffer)
    {
        BufferPool.Return(buffer);
    }

    public bool TryOverrideRemoteAddress(string ip, string hostname)
    {
        if (!string.IsNullOrWhiteSpace(hostname)) _hostname = hostname;

        if (!string.IsNullOrWhiteSpace(ip))
        {
            if (!IPAddress.TryParse(ip, out var parsedAddress)) return false;
            _assignIPAddress(parsedAddress);
        }

        return true;
    }

    private void _assignIPAddress(IPAddress address)
    {
        _ipAddress = address;
        var bytes = _ipAddress.GetAddressBytes();
    
        // For IPv6 (16 bytes)
        if (bytes.Length == 16)
        {
            _idHigh = BitConverter.ToInt64(bytes, 0);
            _idLow = BitConverter.ToInt64(bytes, 8);
        }
        // For IPv4 (4 bytes)
        else
        {
            _idHigh = 0;
            _idLow = BitConverter.ToInt32(bytes, 0);
        }
    
        // Rest of your code...
        var ipAddress = address;
        _address = _socket.RemoteEndPoint != null ? ipAddress.ToString() : string.Empty;
    }

    private StringBuilder _receiveBuffer = new StringBuilder(1024); // Pre-allocate with a reasonable size

    private void Digest(Memory<byte> bytes)
    {
        ReadOnlySpan<byte> span = bytes.Span;

        // Trim null and space characters without allocating a new array
        int start = 0;
        int end = span.Length - 1;

        while (start <= end && (span[start] == 0 || span[start] == ' '))
            start++;

        while (end >= start && (span[end] == 0 || span[end] == ' '))
            end--;

        if (start > end)
            return; // Nothing but whitespace and nulls

        ReadOnlySpan<byte> trimmedSpan = span.Slice(start, end - start + 1);

        if (trimmedSpan.IsEmpty)
            return;

        // Convert to string only once (still required for processing)
        string data = trimmedSpan.ToArray().ToAsciiString();

        // Append to the existing buffer without string concatenation
        _receiveBuffer.Append(data);
        string currentBuffer = _receiveBuffer.ToString();

        bool newLinePending = !currentBuffer.EndsWith('\r') && !currentBuffer.EndsWith('\n');

        // Use StringSplitOptions.None and then filter out empty entries to avoid allocating an array for separator chars
        string[] lines = currentBuffer.Split(new[] { '\r', '\n' });

        int totalLines = newLinePending ? lines.Length - 1 : lines.Length;

        for (int i = 0; i < totalLines; i++)
        {
            string line = lines[i];
            if (!string.IsNullOrEmpty(line))
            {
                OnReceive?.Invoke(this, line);
            }
        }

        // Reset or update the receive buffer
        _receiveBuffer.Clear();
        if (newLinePending)
        {
            _receiveBuffer.Append(lines[^1]);
        }
    }

    private void ReceiveData(SocketAsyncEventArgs socketAsyncEventArgs)
    {
        try
        {
            // This order is do while on purpose
            do
            {
                if (socketAsyncEventArgs.BytesTransferred > 0)
                {
                    Digest(socketAsyncEventArgs.MemoryBuffer.Trim<byte>(0));
                    // Clear buffer for next bit of data
                    socketAsyncEventArgs.MemoryBuffer.Span.Clear();
                }
                else
                {
                    _socket.Close();
                }
            }
            // For all outstanding bytes loop that arent async callback
            while (!_socket.SafeHandle.IsInvalid && _socket.Connected && !_socket.ReceiveAsync(socketAsyncEventArgs));
        }
        catch (ObjectDisposedException)
        {
            // Socket has closed & disposed
            _socket.Close();
        }
        finally
        {
            if (!_socket.Connected)
            {
                OnDisconnect?.Invoke(this, GetId());
                ReturnSocketAsyncEventArgs(socketAsyncEventArgs);
            }
        }
    }
}