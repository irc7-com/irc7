using System.Numerics;

namespace Irc.Interfaces;

public interface IConnection
{
    EventHandler<string>? OnSend { get; set; }
    EventHandler<string>? OnReceive { get; set; }
    EventHandler<long>? OnConnect { get; set; }
    EventHandler<long>? OnDisconnect { get; set; }
    EventHandler<Exception>? OnError { get; set; }

    string GetIp();
    string GetIpAndPort();
    string GetHostname();
    long GetId();
    void Send(string message);
    void Disconnect(string message);
    void Accept();
    bool TryOverrideRemoteAddress(string ip, string hostname);
}