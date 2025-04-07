namespace Irc.Interfaces;

internal interface IExtendedServerObject
{
    void ProcessCookie(IUser user, string name, string value);
}