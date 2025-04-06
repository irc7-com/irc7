using System.Numerics;

namespace Irc7d;

public interface IConnection
{
    EventHandler<string>? OnSend { get; set; }
    EventHandler<string>? OnReceive { get; set; }
    EventHandler<BigInteger>? OnConnect { get; set; }
    EventHandler<BigInteger>? OnDisconnect { get; set; }
    EventHandler<Exception>? OnError { get; set; }

    string GetIp();
    string GetIpAndPort();
    string GetHostname();
    BigInteger GetId();
    void Send(string message);
    void Disconnect(string message);
    void Accept();
    bool TryOverrideRemoteAddress(string ip, string hostname);
}