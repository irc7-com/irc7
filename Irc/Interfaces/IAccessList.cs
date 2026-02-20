using Irc.Access;
using Irc.Enumerations;

namespace Irc.Interfaces;

public interface IAccessList
{
    EnumAccessError Add(AccessEntry accessEntry);
    void PruneExpired();
    EnumAccessError Clear(EnumUserAccessLevel userAccessLevel, EnumAccessLevel accessLevel);
    EnumAccessError Delete(AccessEntry accessEntry);
    List<AccessEntry>? Get(EnumAccessLevel accessLevel);
    AccessEntry? Get(EnumAccessLevel accessLevel, string mask);
    Dictionary<EnumAccessLevel, List<AccessEntry>> GetEntries();
}
