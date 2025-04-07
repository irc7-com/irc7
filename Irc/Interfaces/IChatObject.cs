using Irc.Enumerations;
using Irc.Objects;

namespace Irc.Interfaces;

public interface IChatObject
{
    Guid Id { get; }
    EnumUserAccessLevel Level { get; }
    IModeCollection Modes { get; }
    string Name { get; set; }
    string ShortId { get; }
    IModeCollection GetModes();
    void Send(string message);
    void Send(string message, ChatObject except);
    void Send(string message, EnumChannelAccessLevel accessLevel);
    string ToString();
    bool CanBeModifiedBy(ChatObject source);
}