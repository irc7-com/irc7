namespace Irc.Interfaces;

public interface IChannelModes : IModeCollection
{
    public bool InviteOnly { get; set; }
    public string? Key { get; set; }
    public bool Moderated { get; set; }
    public bool NoExtern { get; set; }
    public bool Private { get; set; }
    public bool Secret { get; set; }
    public bool Hidden { get; set; }
    public bool TopicOp { get; set; }
    public int UserLimit { get; set; }
}