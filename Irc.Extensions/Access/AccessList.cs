using Irc.Enumerations;
using Irc.Extensions.Interfaces;
using System.Threading.Tasks;

namespace Irc.Extensions.Access;

public class AccessList : IAccessList
{
    protected Dictionary<EnumAccessLevel, List<AccessEntry>> AccessEntries = new();

    public EnumAccessError Add(AccessEntry accessEntry)
    {
        var accessList = Get(accessEntry.AccessLevel);
        if (accessList == null) return EnumAccessError.IRCERR_BADLEVEL;

        var entry = accessList.FirstOrDefault(entry => entry.Mask == accessEntry.Mask);
        if (entry != null) return EnumAccessError.IRCERR_DUPACCESS;

        accessList.Add(accessEntry);
        return EnumAccessError.SUCCESS;
    }

    public EnumAccessError Delete(AccessEntry accessEntry)
    {
        var accessList = Get(accessEntry.AccessLevel);
        if (accessList == null) return EnumAccessError.IRCERR_BADLEVEL;

        var entry = accessList.FirstOrDefault(entry => entry.Mask == accessEntry.Mask);
        if (entry == null) return EnumAccessError.IRCERR_MISACCESS;

        accessList.Remove(entry);
        return EnumAccessError.SUCCESS;
    }
    public List<AccessEntry>? Get(EnumAccessLevel accessLevel)
    {
        AccessEntries.TryGetValue(accessLevel, out var list);
        return list;
    }
    public AccessEntry? Get(EnumAccessLevel accessLevel, string mask)
    {
        var accessList = Get(accessLevel);
        if (accessList == null) return null;

        return accessList.FirstOrDefault(entry => entry.Mask == mask);
    }
    public Dictionary<EnumAccessLevel, List<AccessEntry>> GetEntries() => AccessEntries;
    public EnumAccessError Clear(EnumUserAccessLevel userAccessLevel, EnumAccessLevel accessLevel)
    {
        bool hasRemaining = false;
        AccessEntries
            .Where((kvp) => (accessLevel == EnumAccessLevel.All || kvp.Key == accessLevel))
            .ToList()
            .ForEach(
                kvp =>
                {
                    AccessEntries[kvp.Key] = kvp.Value.Where(accessEntry => (accessEntry.EntryLevel > userAccessLevel)).ToList();
                    if (AccessEntries[kvp.Key].Count > 0)
                    {
                        hasRemaining = true;
                    }
                }
            );

        if (hasRemaining)
        {
            return EnumAccessError.IRCERR_INCOMPLETE;
        }
        return EnumAccessError.SUCCESS;
    }
}