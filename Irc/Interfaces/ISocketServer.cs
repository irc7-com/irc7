using Irc7d;

namespace Irc.Interfaces;

public interface ISocketServer
{
    EventHandler<IConnection>? OnClientConnecting { get; set; }
    EventHandler<IConnection>? OnClientConnected { get; set; }
    EventHandler<IConnection>? OnClientDisconnected { get; set; }
    EventHandler<ISocketServer>? OnListen { get; set; }

    int Port { get; }
    int Backlog { get; }
    int MaxConnectionsPerIp { get; }
    int CurrentConnections { get; }
    int BuffSize { get; }
    void Listen();
    void Close();
}