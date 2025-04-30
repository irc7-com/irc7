using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Irc.Helpers;
using Irc.Interfaces;
using NLog.Fluent;

namespace Irc7d;

public class SocketConnection : IConnection
{
    private static readonly ConcurrentBag<SocketAsyncEventArgs> _argsPool = new();
    private static readonly ConcurrentBag<byte[]> _bufferPool = new();
    private const int BufferSize = 512; // 1KB per buffer
    private readonly string _fullAddress = string.Empty;
    private readonly Socket _socket;
    private string _address = string.Empty;
    private string _hostname = string.Empty;
    private BigInteger _id;
    private IPAddress _ipAddress = new(0);
    private string _received = string.Empty;

    public SocketConnection(Socket socket)
    {
        _socket = socket;

        _id = 0;
        if (_socket.RemoteEndPoint != null)
        {
            var remoteEndPoint = (IPEndPoint)_socket.RemoteEndPoint;
            _fullAddress = _socket.RemoteEndPoint != null
                ? remoteEndPoint.ToString()
                : string.Empty;
            _assignIPAddress(remoteEndPoint.Address);
        }
    }

    public EventHandler<string>? OnSend { get; set; }
    public EventHandler<string>? OnReceive { get; set; }
    public EventHandler<BigInteger>? OnConnect { get; set; }
    public EventHandler<BigInteger>? OnDisconnect { get; set; }
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

    public BigInteger GetId()
    {
        return _id;
    }

    public void Send(string message)
    {
        var sendAsync = new SocketAsyncEventArgs();
        sendAsync.SetBuffer(message.ToByteArray());
        sendAsync.Completed += (sender, args) => OnSend?.Invoke(this, message);

        if (!_socket.Connected) OnDisconnect?.Invoke(this, GetId());

        if (_socket.Connected)
        {
            try
            {
                if (!_socket.SendAsync(sendAsync)) // Report data is sent
                    OnSend?.Invoke(this, message.Substring(sendAsync.Offset, sendAsync.BytesTransferred));
            }
            catch (ObjectDisposedException)
            {
                Log.Warn("Socket connection has been disposed.");
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, ex);
            }
        }
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
        recvAsync.Completed += (sender, args) => { ReceiveData(args); };
        // If Sync receive from connect then process data
        if (!_socket.ReceiveAsync(recvAsync)) ReceiveData(recvAsync);
    }
    
    private static SocketAsyncEventArgs GetSocketAsyncEventArgs()
    {
        if (!_argsPool.TryTake(out var args) && args?.LastOperation != SocketAsyncOperation.None)
        {
            args = new SocketAsyncEventArgs();
            args.SetBuffer(GetBuffer(), 0, BufferSize);
            return args;
        }

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

        try
        {
            args.SetBuffer(null, 0, 0);
            args.Completed -= null;
            _argsPool.Add(args);
        }
        catch (Exception ex)
        {
            Log.Warn("Error trying to reset SocketAsyncEventArgs");
        }
    }

    private static byte[] GetBuffer()
    {
        if (!_bufferPool.TryTake(out var buffer))
        {
            buffer = new byte[BufferSize];
        }
        return buffer;
    }

    private static void ReturnBuffer(byte[] buffer)
    {
        _bufferPool.Add(buffer);
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
        var remoteAddressBytes = _ipAddress.GetAddressBytes();
        _id = new BigInteger(remoteAddressBytes);

        var ipAddress = address;

        _address = _socket.RemoteEndPoint != null
            ? ipAddress.ToString()
            : string.Empty;
    }

    private void Digest(Memory<byte> bytes)
    {
        var data = bytes.ToArray().ToAsciiString();
        data = data.Trim('\0', ' ');
        if (data.Length > 0)
        {
            _received = $"{_received}{data}";

            var bNewLinePending = !_received.EndsWith('\r') && !_received.EndsWith('\n');

            var lines = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var totalLines = bNewLinePending ? lines.Length - 1 : lines.Length;

            for (var i = 0; i < totalLines; i++) OnReceive?.Invoke(this, lines[i]);

            if (bNewLinePending) _received = lines[^1];
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