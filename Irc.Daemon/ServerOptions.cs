namespace Irc7d;

public class ServerOptions
{
    public string? ConfigPath { get; set; }
    public string? BindIp { get; set; }
    public int BindPort { get; set; }
    public int Backlog { get; set; }
    public int BufferSize { get; set; }
    public int MaxConnections { get; set; }
    public string? Fqdn { get; set; }
    public string? ServerType { get; set; }
    public string? ChatServerIp { get; set; }
}