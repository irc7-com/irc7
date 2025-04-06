using Irc.Security;

namespace Irc.Interfaces;

public interface ISecurityManager
{
    void AddSupportPackage(SupportPackage supportPackage);
    SupportPackage CreatePackageInstance(string name, ICredentialProvider credentialProvider);
    string GetSupportedPackages();
}