using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using NLog;

namespace Irc7d;

public class SocketServer : Socket, ISocketServer
{
    public static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ConcurrentDictionary<BigInteger, ConcurrentBag<IConnection>> Sockets = new();


    public SocketServer(IPAddress ip, int port, int backlog, int maxConnectionsPerIp, int buffSize) : base(
        SocketType.Stream, ProtocolType.Tcp)
    {
        Ip = ip;
        Port = port;
        Backlog = backlog;
        MaxConnectionsPerIp = maxConnectionsPerIp;
        BuffSize = buffSize;
        BuffSize = buffSize;
    }

    public IPAddress Ip { get; }
    public EventHandler<IConnection>? OnClientConnecting { get; set; }
    public EventHandler<IConnection>? OnClientConnected { get; set; }
    public EventHandler<IConnection>? OnClientDisconnected { get; set; }
    public EventHandler<ISocketServer>? OnListen { get; set; }
    public int Port { get; }
    public int Backlog { get; }
    public int MaxConnectionsPerIp { get; }
    public int BuffSize { get; }
    public int CurrentConnections { get; } = 0;

    public void Listen()
    {
        Bind(new IPEndPoint(Ip, Port));
        Listen(Backlog);

        // Callback for OnListen
        OnListen?.Invoke(this, this);

        var acceptAsync = new SocketAsyncEventArgs();
        acceptAsync.Completed += (sender, args) => { AcceptLoop(args); };

        // Get first socket
        AcceptAsync(acceptAsync);
    }

    public void Close()
    {
        Close();
    }


    public void AcceptConnection(Socket acceptSocket)
    {
        var connection = new SocketConnection(acceptSocket) as IConnection;
        OnClientConnecting?.Invoke(this, connection);
    }

    public void AcceptLoop(SocketAsyncEventArgs args)
    {
        do
        {
            if (args.AcceptSocket != null) AcceptConnection(args.AcceptSocket);
            // Get next socket
            // Reset AcceptSocket for next accept
            args.AcceptSocket = null;
        } while (!AcceptAsync(args));
    }

    public void Accept(IConnection connection)
    {
        if (Sockets.ContainsKey(connection.GetId()))
        {
            connection.Disconnect(
                "Too many connections"
            );
            return;
        }

        connection.OnDisconnect += ClientDisconnected;

        var socketCollection = Sockets.GetOrAdd(connection.GetId(), new ConcurrentBag<IConnection>());
        Log.Info($"Current keys: {Sockets.Count} / Current sockets: {socketCollection.Count}");

        socketCollection.Add(connection);
        connection.Accept();

        OnClientConnected?.Invoke(this, connection);
    }

    private void ClientDisconnected(object? sender, BigInteger bigIP)
    {
        if (!Sockets.ContainsKey(bigIP))
        {
            Log.Error($"ClientDisconnected: Client {bigIP} is not connected");
        }

        var bag = Sockets[bigIP];
        bag.TryTake(out var connection);

        if (connection == null)
        {
            Log.Info(
                $"{sender}[{bigIP}] has disconnected but failed to TryTake / total: {Sockets.Count} ");
            return;
        }

        OnClientDisconnected?.Invoke(this, connection);
    }
}