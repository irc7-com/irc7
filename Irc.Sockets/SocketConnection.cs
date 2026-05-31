using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Irc.Helpers;
using Irc.Interfaces;
using NLog;

namespace Irc.Host;

public class SocketConnection : IConnection
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
            AssignIPAddress(remoteEndPoint.Address);
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
        sendAsync.Completed += (_, args) =>
        {
            OnSend?.Invoke(this, message);
            args.Dispose();
        };

        if (!_socket.Connected) OnDisconnect?.Invoke(this, GetId());

        if (_socket.Connected)
        {
            if (!_socket.SendAsync(sendAsync))
            {
                OnSend?.Invoke(this, message.Substring(sendAsync.Offset, sendAsync.BytesTransferred));
                sendAsync.Dispose();
            }
        }
        else
        {
            sendAsync.Dispose();
        }

        if (Irc.Logging.Logging.TraceEnabled)
        {
            var line = $"Send[{_address}]: {message}";
            Log.Trace(line);
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

    private SocketAsyncEventArgs? _recvAsync;

    public void Accept()
    {
        _recvAsync = new SocketAsyncEventArgs();
        _recvAsync.UserToken = GetId();
        _recvAsync.SetBuffer(new byte[1024]);
        _recvAsync.Completed += (_, args) => { ReceiveData(args); };
        if (!_socket.ReceiveAsync(_recvAsync)) ReceiveData(_recvAsync);
    }

    public bool TryOverrideRemoteAddress(string ip, string hostname)
    {
        if (!string.IsNullOrWhiteSpace(hostname)) _hostname = hostname;

        if (!string.IsNullOrWhiteSpace(ip))
        {
            if (!IPAddress.TryParse(ip, out var parsedAddress)) return false;
            AssignIPAddress(parsedAddress);
        }

        return true;
    }

    private void AssignIPAddress(IPAddress address)
    {
        _ipAddress = address;
        var remoteAddressBytes = _ipAddress.GetAddressBytes();
        _id = new BigInteger(remoteAddressBytes);

        _address = _socket.RemoteEndPoint != null ? address.ToString() : string.Empty;
    }

    private void Digest(Memory<byte> bytes)
    {
        var data = bytes.ToArray().ToAsciiString();
        data = data.Trim('\0', ' ');
        if (data.Length > 0)
        {
            if (Irc.Logging.Logging.TraceEnabled)
            {
                var line = $"Recv[{_address}]: {data}";
                Log.Trace(line);
            }
            _received = $"{_received}{data}";

            if (_received.Length > 1024)
            {
                Disconnect("Line too long");
                return;
            }

            var isNewLinePending = !_received.EndsWith('\r') && !_received.EndsWith('\n');
            var lines = _received.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var totalLines = isNewLinePending ? lines.Length - 1 : lines.Length;

            for (var i = 0; i < totalLines; i++) OnReceive?.Invoke(this, lines[i]);

            _received = isNewLinePending ? lines[^1] : string.Empty;
        }
    }

    private void ReceiveData(SocketAsyncEventArgs socketAsyncEventArgs)
    {
        try
        {
            do
            {
                if (socketAsyncEventArgs.BytesTransferred > 0)
                {
                    Digest(socketAsyncEventArgs.MemoryBuffer.Trim<byte>(0));
                    socketAsyncEventArgs.MemoryBuffer.Span.Clear();
                }
                else
                {
                    _socket.Close();
                }
            }
            while (!_socket.SafeHandle.IsInvalid && _socket.Connected && !_socket.ReceiveAsync(socketAsyncEventArgs));
        }
        catch (ObjectDisposedException)
        {
            _socket.Close();
        }
        finally
        {
            if (!_socket.Connected)
            {
                _recvAsync?.Dispose();
                _recvAsync = null;
                OnDisconnect?.Invoke(this, GetId());
            }
        }
    }
}

