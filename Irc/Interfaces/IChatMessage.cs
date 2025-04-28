namespace Irc.Interfaces;

public interface IChatMessage
{
    List<string> Parameters { get; }
    string OriginalText { get; }
    string GetPrefix { get; }
    bool HasCommand { get; }
    ICommand? GetCommand();
    string GetCommandName();
    List<string> GetParameters();
}