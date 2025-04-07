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

    public ChannelModes()
    {
        // IRC Modes

        Modes.Add(Resources.MemberModeHost, new Operator());
        Modes.Add(Resources.MemberModeVoice, new Voice());
        Modes.Add(Resources.ChannelModePrivate, new Private());
        Modes.Add(Resources.ChannelModeSecret, new Secret());
        Modes.Add(Resources.ChannelModeHidden, new Hidden());
        Modes.Add(Resources.ChannelModeInvite, new InviteOnly());
        Modes.Add(Resources.ChannelModeTopicOp, new TopicOp());
        Modes.Add(Resources.ChannelModeNoExtern, new NoExtern());
        Modes.Add(Resources.ChannelModeModerated, new Moderated());
        Modes.Add(Resources.ChannelModeUserLimit, new UserLimit());
        Modes.Add(Resources.ChannelModeBan, new BanList());
        Modes.Add(Resources.ChannelModeKey, new Key());

        // IRCX Modes
        Modes.Add(Resources.ChannelModeAuthOnly, new AuthOnly());
        Modes.Add(Resources.ChannelModeProfanity, new NoFormat());
        Modes.Add(Resources.ChannelModeRegistered, new Registered());
        Modes.Add(Resources.ChannelModeKnock, new Knock());
        Modes.Add(Resources.ChannelModeNoWhisper, new NoWhisper());
        Modes.Add(Resources.ChannelModeAuditorium, new Auditorium());
        Modes.Add(Resources.ChannelModeCloneable, new Cloneable());
        Modes.Add(Resources.ChannelModeClone, new Clone());
        Modes.Add(Resources.ChannelModeService, new Service());
        Modes.Add(Resources.MemberModeOwner, new Owner());

        // Apollo Modes
        Modes.Add(Resources.ChannelModeNoGuestWhisper, new NoGuestWhisper());
        Modes.Add(Resources.ChannelModeOnStage, new OnStage());
        Modes.Add(Resources.ChannelModeSubscriber, new Subscriber());
    }

    // IRCX Modes

    public bool Auditorium
    {
        get => Modes[Resources.ChannelModeAuditorium].Get() == 1;
        set => Modes[Resources.ChannelModeAuditorium].Set(Convert.ToInt32(value));
    }

    public bool NoGuestWhisper
    {
        get => Modes[Resources.ChannelModeNoGuestWhisper].Get() == 1;
        set => Modes[Resources.ChannelModeNoGuestWhisper].Set(Convert.ToInt32(value));
    }

    public bool AuthOnly
    {
        get => Modes[Resources.ChannelModeAuthOnly].Get() == 1;
        set => Modes[Resources.ChannelModeAuthOnly].Set(Convert.ToInt32(value));
    }

    public bool Profanity
    {
        get => Modes[Resources.ChannelModeProfanity].Get() == 1;
        set => Modes[Resources.ChannelModeProfanity].Set(Convert.ToInt32(value));
    }

    public bool Registered
    {
        get => Modes[Resources.ChannelModeRegistered].Get() == 1;
        set => Modes[Resources.ChannelModeRegistered].Set(Convert.ToInt32(value));
    }

    public bool Knock
    {
        get => Modes[Resources.ChannelModeKnock].Get() == 1;
        set => Modes[Resources.ChannelModeKnock].Set(Convert.ToInt32(value));
    }

    public bool NoWhisper
    {
        get => Modes[Resources.ChannelModeNoWhisper].Get() == 1;
        set => Modes[Resources.ChannelModeNoWhisper].Set(Convert.ToInt32(value));
    }

    public bool Cloneable
    {
        get => Modes[Resources.ChannelModeCloneable].Get() == 1;
        set => Modes[Resources.ChannelModeCloneable].Set(Convert.ToInt32(value));
    }

    public bool Clone
    {
        get => Modes[Resources.ChannelModeClone].Get() == 1;
        set => Modes[Resources.ChannelModeClone].Set(Convert.ToInt32(value));
    }

    public bool Service
    {
        get => Modes[Resources.ChannelModeService].Get() == 1;
        set => Modes[Resources.ChannelModeService].Set(Convert.ToInt32(value));
    }

    // Apollo Modes
    public bool OnStage
    {
        get => Modes[Resources.ChannelModeOnStage].Get() == 1;
        set => Modes[Resources.ChannelModeOnStage].Set(Convert.ToInt32(value));
    }

    public bool Subscriber
    {
        get => Modes[Resources.ChannelModeSubscriber].Get() == 1;
        set => Modes[Resources.ChannelModeSubscriber].Set(Convert.ToInt32(value));
    }

    // IRC Modes

    public bool InviteOnly
    {
        get => Modes[Resources.ChannelModeInvite].Get() == 1;
        set => Modes[Resources.ChannelModeInvite].Set(Convert.ToInt32(value));
    }

    public string? Key
    {
        get => Keypass;
        set
        {
            var hasKey = !string.IsNullOrWhiteSpace(value);
            Modes[Resources.ChannelModeKey].Set(hasKey);
            Keypass = value;
        }
    }

    public bool Moderated
    {
        get => Modes[Resources.ChannelModeModerated].Get() == 1;
        set => Modes[Resources.ChannelModeModerated].Set(Convert.ToInt32(value));
    }

    public bool NoExtern
    {
        get => Modes[Resources.ChannelModeNoExtern].Get() == 1;
        set => Modes[Resources.ChannelModeNoExtern].Set(Convert.ToInt32(value));
    }

    public bool Private
    {
        get => Modes[Resources.ChannelModePrivate].Get() == 1;
        set => Modes[Resources.ChannelModePrivate].Set(Convert.ToInt32(value));
    }

    public bool Secret
    {
        get => Modes[Resources.ChannelModeSecret].Get() == 1;
        set => Modes[Resources.ChannelModeSecret].Set(Convert.ToInt32(value));
    }

    public bool Hidden
    {
        get => Modes[Resources.ChannelModeHidden].Get() == 1;
        set => Modes[Resources.ChannelModeHidden].Set(Convert.ToInt32(value));
    }

    public bool TopicOp
    {
        get => Modes[Resources.ChannelModeTopicOp].Get() == 1;
        set => Modes[Resources.ChannelModeTopicOp].Set(Convert.ToInt32(value));
    }

    public int UserLimit
    {
        get => Modes[Resources.ChannelModeUserLimit].Get();
        set => Modes[Resources.ChannelModeUserLimit].Set(value);
    }

    public override string ToString()
    {
        // TODO: <MODESTRING> Fix the below for Limit and Key on mode string
        var limit = Modes['l'].Get() > 0 ? $" {Modes['l'].Get()}" : string.Empty;
        var key = Modes['k'].Get() != 0 ? $" {Keypass}" : string.Empty;

        return
            $"{new string(Modes.Where(mode => mode.Value.Get() > 0).Select(mode => mode.Key).ToArray())}{limit}{key}";
    }
}