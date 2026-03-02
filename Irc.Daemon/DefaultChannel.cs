namespace Irc7d;

public class DefaultChannel
{
    public string Name { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Dictionary<char, int> Modes { get; set; } = new();
    public Dictionary<string, string> Props { get; set; } = new();
}