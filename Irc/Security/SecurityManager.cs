using Irc.Interfaces;
using Irc.Security.Sspi;

namespace Irc.Security;

public class SecurityManager : ISecurityManager
{
    private SupportPackage _supportProvider;
    
    public SecurityManager(SupportPackage supportPackage)
    {
        _supportProvider = supportPackage;
        IrcxSspiModule.Initialize();
    }
    
    public SupportPackage GetSupportPackage()
    {
        return _supportProvider;
    }

    public string GetSupportedPackages()
    {
        return "GateKeeper,NTLM";
    }
}