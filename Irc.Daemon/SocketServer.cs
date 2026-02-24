using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Irc.Interfaces;
using NLog;

namespace Irc7d;

public class SocketServer : Socket, ISocketServer
{
    public static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ConcurrentDictionary<BigInteger, ConcurrentDictionary<IConnection, byte>> Sockets = new();


    public SocketServer(IPAddress ip, int port, int backlog, int maxConnections, int maxConnectionsPerIp, int buffSize) : base(
        SocketType.Stream, ProtocolType.Tcp)
    {
        Ip = ip;
        Port = port;
        Backlog = backlog;
        MaxConnections = maxConnections;
        MaxConnectionsPerIp = maxConnectionsPerIp;
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
    public int MaxConnections { get; }
    public int CurrentConnections { get; private set; } = 0;

    public new void Listen()
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

    public new void Close()
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
        if (MaxConnections > 0 && CurrentConnections >= MaxConnections)
        {
            connection.Disconnect("Server is full");
            return;
        }

        if (Sockets.TryGetValue(connection.GetId(), out var existingBag) && MaxConnectionsPerIp > 0 && existingBag.Count >= MaxConnectionsPerIp)
        {
            connection.Disconnect(
                "Too many connections"
            );
            return;
        }

        connection.OnDisconnect += ClientDisconnected;

        var socketCollection = Sockets.GetOrAdd(connection.GetId(), new ConcurrentDictionary<IConnection, byte>());
        Log.Info($"Current keys: {Sockets.Count} / Current sockets: {socketCollection.Count}");

        socketCollection.TryAdd(connection, 0);
        CurrentConnections++;
        connection.Accept();

        OnClientConnected?.Invoke(this, connection);
    }

    private void ClientDisconnected(object? sender, BigInteger bigIP)
    {
        if (sender is not IConnection connection) return;

        if (!Sockets.TryGetValue(bigIP, out var socketCollection))
        {
            Log.Error($"ClientDisconnected: Client {bigIP} is not in the sockets collection");
            return;
        }

        if (!socketCollection.TryRemove(connection, out _))
        {
            Log.Info(
                $"{sender}[{bigIP}] has disconnected but failed to remove from collection / total: {Sockets.Count} ");
            return;
        }

        CurrentConnections--;

        // Clean up empty dictionary
        if (socketCollection.IsEmpty)
        {
            Sockets.TryRemove(bigIP, out _);
        }

        OnClientDisconnected?.Invoke(this, connection);
    }
}