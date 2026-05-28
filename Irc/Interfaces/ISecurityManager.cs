using Irc.Security;

namespace Irc.Interfaces;

public interface ISecurityManager
{
    SupportPackage GetSupportPackage();
    string GetSupportedPackages();
}