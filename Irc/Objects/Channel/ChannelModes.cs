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
    public Private Private { get; } = new Private();
    public Secret Secret { get; } = new Secret();
    public Hidden Hidden { get; } = new Hidden();
    public InviteOnly InviteOnly { get; } = new InviteOnly();
    public TopicOp TopicOp { get; } = new TopicOp();
    public NoExtern NoExtern { get; } = new NoExtern();
    public Moderated Moderated { get; } = new Moderated();
    public UserLimit UserLimit { get; } = new UserLimit();
    public BanList BanList { get; } = new BanList();
    public Key Key { get; } = new Key();
    
    public AuthOnly AuthOnly { get; } = new AuthOnly();
    public NoFormat Profanity { get; } = new NoFormat();
    public Registered Registered { get; } = new Registered();
    public Knock Knock { get; } = new Knock();
    public NoWhisper NoWhisper { get; } = new NoWhisper();
    public Auditorium Auditorium { get; } = new Auditorium();
    public Cloneable Cloneable { get; } = new Cloneable();
    public Clone Clone { get; } = new Clone();
    public Service Service { get; } = new Service();
    public OwnerRule OwnerRule { get; } = new OwnerRule();
    
    public NoGuestWhisper NoGuestWhisper { get; } = new NoGuestWhisper();
    public OnStage OnStage { get; } = new OnStage();
    public Subscriber Subscriber { get; } = new Subscriber();
    
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