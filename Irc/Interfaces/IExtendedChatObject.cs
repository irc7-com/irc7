namespace Irc.Interfaces;

public interface IExtendedChatObject : IChatObject
{
    IPropCollection PropCollection { get; }
    IAccessList AccessList { get; }
}