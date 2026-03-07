using Irc.Access;
using Irc.Enumerations;

namespace Irc.Interfaces;

public interface IAccessList
{
    EnumAccessError Add(AccessEntry accessEntry);
    void PruneExpired();
    EnumAccessError Clear(IChatObject source, IChatObject target, EnumUserAccessLevel userAccessLevel,
        EnumAccessLevel accessLevel);
    EnumAccessError Delete(AccessEntry accessEntry);
    List<AccessEntry>? Get(EnumAccessLevel accessLevel);
    AccessEntry? Get(EnumAccessLevel accessLevel, string mask);
    Dictionary<EnumAccessLevel, List<AccessEntry>> GetEntries();

    public bool CanModifyAccessLevel(IChatObject source,
        IChatObject target,
        EnumAccessLevel accessLevel);

    public bool CanModifyAccessEntry(IChatObject source,
        IChatObject target,
        AccessEntry entry);
}
