using Irc.Extensions.Interfaces;
using Irc.Extensions.Modes.Channel;
using Irc.Modes.Channel.Member;
using Irc.Objects;

namespace Irc.Extensions.Objects.Channel;

public class ExtendedChannelModes : ChannelModes, IExtendedChannelModes
{
    public ExtendedChannelModes()
    {
        Modes.Add(ExtendedResources.ChannelModeAuthOnly, new AuthOnly());
        Modes.Add(ExtendedResources.ChannelModeProfanity, new NoFormat());
        Modes.Add(ExtendedResources.ChannelModeRegistered, new Registered());
        Modes.Add(ExtendedResources.ChannelModeKnock, new Knock());
        Modes.Add(ExtendedResources.ChannelModeNoWhisper, new NoWhisper());
        Modes.Add(ExtendedResources.ChannelModeAuditorium, new Auditorium());
        Modes.Add(ExtendedResources.ChannelModeCloneable, new Cloneable());
        Modes.Add(ExtendedResources.ChannelModeClone, new Clone());
        Modes.Add(ExtendedResources.ChannelModeService, new Service());
        Modes.Add(ExtendedResources.MemberModeOwner, new Owner());
    }

    public bool Auditorium
    {
        get => Modes[ExtendedResources.ChannelModeAuditorium].Get() == 1;
        set => Modes[ExtendedResources.ChannelModeAuditorium].Set(Convert.ToInt32(value));
    }

    public bool NoGuestWhisper
    {
        get => Modes[ExtendedResources.ChannelModeNoGuestWhisper].Get() == 1;
        set => Modes[ExtendedResources.ChannelModeNoGuestWhisper].Set(Convert.ToInt32(value));
    }

    public bool AuthOnly
    {
        get => Modes[ExtendedResources.ChannelModeAuthOnly].Get() == 1;
        set => Modes[ExtendedResources.ChannelModeAuthOnly].Set(Convert.ToInt32(value));
    }

    public bool Profanity
    {
        get => Modes[ExtendedResources.ChannelModeProfanity].Get() == 1;
        set => Modes[ExtendedResources.ChannelModeProfanity].Set(Convert.ToInt32(value));
    }

    public bool Registered
    {
        get => Modes[ExtendedResources.ChannelModeRegistered].Get() == 1;
        set => Modes[ExtendedResources.ChannelModeRegistered].Set(Convert.ToInt32(value));
    }

    public bool Knock
    {
        get => Modes[ExtendedResources.ChannelModeKnock].Get() == 1;
        set => Modes[ExtendedResources.ChannelModeKnock].Set(Convert.ToInt32(value));
    }

    public bool NoWhisper
    {
        get => Modes[ExtendedResources.ChannelModeNoWhisper].Get() == 1;
        set => Modes[ExtendedResources.ChannelModeNoWhisper].Set(Convert.ToInt32(value));
    }

    public bool Cloneable
    {
        get => Modes[ExtendedResources.ChannelModeCloneable].Get() == 1;
        set => Modes[ExtendedResources.ChannelModeCloneable].Set(Convert.ToInt32(value));
    }

    public bool Clone
    {
        get => Modes[ExtendedResources.ChannelModeClone].Get() == 1;
        set => Modes[ExtendedResources.ChannelModeClone].Set(Convert.ToInt32(value));
    }

    public bool Service
    {
        get => Modes[ExtendedResources.ChannelModeService].Get() == 1;
        set => Modes[ExtendedResources.ChannelModeService].Set(Convert.ToInt32(value));
    }
}