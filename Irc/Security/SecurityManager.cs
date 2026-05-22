using Irc.Interfaces;
using Irc.Security.Sspi;

namespace Irc.Security;

public class SecurityManager : ISecurityManager
{
    private readonly Dictionary<string, SupportPackage> _supportProviders =
        new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, PermissionProfile> _permissionProfiles;

    public SecurityManager()
        : this(null)
    {
    }

    public SecurityManager(Dictionary<string, PermissionProfile>? permissionProfiles)
    {
        IrcxSspiModule.Initialize();
        _permissionProfiles = permissionProfiles != null
            ? new Dictionary<string, PermissionProfile>(permissionProfiles, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, PermissionProfile>(StringComparer.OrdinalIgnoreCase);
    }
    
    public void AddSupportPackage(SupportPackage supportPackage)
    {
        _supportProviders.Add(supportPackage.GetType().Name, supportPackage);
        UpdateSupportPackages();
    }

    public SupportPackage CreatePackageInstance(ICredentialProvider credentialProvider)
    {
        // if (!_supportProviders.TryGetValue(name, out var supportPackage))
        //     throw new InvalidOperationException($"No support package found for {name}");

        return new SupportPackage(credentialProvider, _permissionProfiles);
    }

    public string GetSupportedPackages()
    {
        return "GateKeeper,NTLM";
    }

    private void UpdateSupportPackages()
    {
        // _supportedPackages = string.Join(',',
        //     _supportProviders.Where(provider => provider.Value.Listed)
        //         .Select(provider => provider.Value.GetPackageName()).Reverse());
    }
}