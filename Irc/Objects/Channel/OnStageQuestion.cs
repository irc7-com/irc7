namespace Irc.Objects.Channel;

public sealed record OnStageQuestion(
    int Id,
    string Nickname,
    string SourceRoom,
    string Message,
    DateTime SubmittedAtUtc)
{
    public string ToEventData()
    {
        return $"{Id} {Nickname} {SourceRoom} :{Message}";
    }
}
