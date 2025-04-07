namespace Irc.Interfaces;

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