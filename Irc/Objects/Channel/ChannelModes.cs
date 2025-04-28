using Irc.Constants;
using Irc.Interfaces;
using Irc.Modes.Channel;
using Irc.Modes.Channel.Member;
using Irc.Objects.Collections;

namespace Irc.Objects.Channel;



public class ChannelModes : ModeCollection, IChannelModes
{
    /*
o - give/take channel operator privileges;
p - private channel flag;
s - secret channel flag;
i - invite-only channel flag;
t - topic settable by channel operator only flag;
n - no messages to channel from clients on the outside;
m - moderated channel;
l - set the user limit to channel;
b - set a ban mask to keep users out;
v - give/take the ability to speak on a moderated channel;
k - set a channel key (password).
*/
    public string Keypass { get; set; } = string.Empty;

    public OperatorRule Operator { get; } = new OperatorRule();
    public VoiceRule Voice { get; } = new VoiceRule();
    public PrivateRule Private { get; } = new PrivateRule();
    public SecretRule Secret { get; } = new SecretRule();
    public HiddenRule Hidden { get; } = new HiddenRule();
    public InviteOnlyRule InviteOnly { get; } = new InviteOnlyRule();
    public TopicOpRule TopicOp { get; } = new TopicOpRule();
    public NoExternRule NoExtern { get; } = new NoExternRule();
    public ModeratedRule Moderated { get; } = new ModeratedRule();
    public UserLimitRule UserLimit { get; } = new UserLimitRule();
    public BanListRule BanList { get; } = new BanListRule();
    public KeyRule Key { get; } = new KeyRule();
    
    public AuthOnlyRule AuthOnly { get; } = new AuthOnlyRule();
    public NoFormatRule Profanity { get; } = new NoFormatRule();
    public RegisteredRule Registered { get; } = new RegisteredRule();
    public KnockRule Knock { get; } = new KnockRule();
    public NoWhisperRule NoWhisper { get; } = new NoWhisperRule();
    public AuditoriumRule Auditorium { get; } = new AuditoriumRule();
    public CloneableRule Cloneable { get; } = new CloneableRule();
    public CloneRule Clone { get; } = new CloneRule();
    public ServiceRule Service { get; } = new ServiceRule();
    public OwnerRule OwnerRule { get; } = new OwnerRule();
    
    public NoGuestWhisperRule NoGuestWhisper { get; } = new NoGuestWhisperRule();
    public OnStageRule OnStage { get; } = new OnStageRule();
    public SubscriberRule Subscriber { get; } = new SubscriberRule();
    
    public ChannelModes()
    {
        // IRC Modes
        Modes.Add(Resources.MemberModeHost, Operator);
        Modes.Add(Resources.MemberModeVoice, Voice);
        Modes.Add(Resources.ChannelModePrivate, Private);
        Modes.Add(Resources.ChannelModeSecret, Secret);
        Modes.Add(Resources.ChannelModeHidden, Hidden);
        Modes.Add(Resources.ChannelModeInvite, InviteOnly);
        Modes.Add(Resources.ChannelModeTopicOp, TopicOp);
        Modes.Add(Resources.ChannelModeNoExtern, NoExtern);
        Modes.Add(Resources.ChannelModeModerated, Moderated);
        Modes.Add(Resources.ChannelModeUserLimit, UserLimit);
        Modes.Add(Resources.ChannelModeBan, BanList);
        Modes.Add(Resources.ChannelModeKey, Key);

        // IRCX Modes
        Modes.Add(Resources.ChannelModeAuthOnly, AuthOnly);
        Modes.Add(Resources.ChannelModeProfanity, Profanity);
        Modes.Add(Resources.ChannelModeRegistered, Registered);
        Modes.Add(Resources.ChannelModeKnock, Knock);
        Modes.Add(Resources.ChannelModeNoWhisper, NoWhisper);
        Modes.Add(Resources.ChannelModeAuditorium, Auditorium);
        Modes.Add(Resources.ChannelModeCloneable, Cloneable);
        Modes.Add(Resources.ChannelModeClone, Clone);
        Modes.Add(Resources.ChannelModeService, Service);
        Modes.Add(Resources.MemberModeOwner, OwnerRule);

        // Apollo Modes
        Modes.Add(Resources.ChannelModeNoGuestWhisper, NoGuestWhisper);
        Modes.Add(Resources.ChannelModeOnStage, OnStage);
        Modes.Add(Resources.ChannelModeSubscriber, Subscriber);
    }

    public override string ToString()
    {
        // TODO: <MODESTRING> Fix the below for Limit and Key on mode string
        var limit = UserLimit.ModeValue ? $" {UserLimit.Value}" : string.Empty;
        var key = Key.ModeValue ? $" {Keypass}" : string.Empty;

        return
            $"{new string(Modes.Where(mode => mode.Value.Get() > 0).Select(mode => mode.Key).ToArray())}{limit}{key}";
    }
}