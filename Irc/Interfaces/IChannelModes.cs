using Irc.Modes.Channel;
using Irc.Modes.Channel.Member;

namespace Irc.Interfaces;

public interface IChannelModes : IModeCollection
{
    string Keypass { get; set; }
    OperatorRule Operator { get; }
    VoiceRule Voice { get; }
    Private Private { get; }
    Secret Secret { get; }
    Hidden Hidden { get; }
    InviteOnly InviteOnly { get; }
    TopicOp TopicOp { get; }
    NoExtern NoExtern { get; }
    Moderated Moderated { get; }
    UserLimit UserLimit { get; }
    BanList BanList { get; }
    Key Key { get; }
    AuthOnly AuthOnly { get; }
    NoFormat Profanity { get; }
    Registered Registered { get; }
    Knock Knock { get; }
    NoWhisper NoWhisper { get; }
    Auditorium Auditorium { get; }
    Cloneable Cloneable { get; }
    Clone Clone { get; }
    Service Service { get; }
    OwnerRule OwnerRule { get; }
    NoGuestWhisper NoGuestWhisper { get; }
    OnStage OnStage { get; }
    Subscriber Subscriber { get; }
}