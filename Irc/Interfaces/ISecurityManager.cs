using Irc.Security;

namespace Irc.Interfaces;

public interface ISecurityManager
{
    SaslHandler GetSupportPackage();
    string GetSupportedPackages();
}