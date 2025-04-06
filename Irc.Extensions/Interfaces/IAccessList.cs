using Irc.Enumerations;
using Irc.Extensions.Access;

namespace Irc.Extensions.Interfaces
{
    public interface IAccessList
    {
        EnumAccessError Add(AccessEntry accessEntry);
        EnumAccessError Clear(EnumUserAccessLevel userAccessLevel, EnumAccessLevel accessLevel);
        EnumAccessError Delete(AccessEntry accessEntry);
        List<AccessEntry>? Get(EnumAccessLevel accessLevel);
        AccessEntry? Get(EnumAccessLevel accessLevel, string mask);
        Dictionary<EnumAccessLevel, List<AccessEntry>> GetEntries();
    }
}