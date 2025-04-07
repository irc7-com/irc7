namespace Irc.Interfaces;

public interface IApolloChannelModes : IChannelModes
{
    bool OnStage { get; set; }
    bool Subscriber { get; set; }
}

public interface IExtendedChannelModes
{
    bool AuthOnly { get; set; }
    bool Profanity { get; set; }
    bool Registered { get; set; }
    bool Knock { get; set; }
    bool NoWhisper { get; set; }
    bool NoGuestWhisper { get; set; }
    bool Cloneable { get; set; }
    bool Clone { get; set; }
    bool Service { get; set; }
}

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