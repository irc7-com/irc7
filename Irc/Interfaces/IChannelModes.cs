using Irc.Modes.Channel;
using Irc.Modes.Channel.Member;

namespace Irc.Interfaces;

public interface IChannelModes : IModeCollection
{
    string Keypass { get; set; }
    OperatorRule Operator { get; }
    VoiceRule Voice { get; }
    PrivateRule Private { get; }
    SecretRule Secret { get; }
    HiddenRule Hidden { get; }
    InviteOnlyRule InviteOnly { get; }
    TopicOpRule TopicOp { get; }
    NoExternRule NoExtern { get; }
    ModeratedRule Moderated { get; }
    UserLimitRule UserLimit { get; }
    BanListRule BanList { get; }
    KeyRule Key { get; }
    AuthOnlyRule AuthOnly { get; }
    NoFormatRule Profanity { get; }
    RegisteredRule Registered { get; }
    KnockRule Knock { get; }
    NoWhisperRule NoWhisper { get; }
    AuditoriumRule Auditorium { get; }
    CloneableRule Cloneable { get; }
    CloneRule Clone { get; }
    ServiceRule Service { get; }
    OwnerRule OwnerRule { get; }
    NoGuestWhisperRule NoGuestWhisper { get; }
    OnStageRule OnStage { get; }
    SubscriberRule Subscriber { get; }
}