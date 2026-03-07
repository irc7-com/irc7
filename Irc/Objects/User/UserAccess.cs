using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Access.User;

public class UserAccess : AccessList
{
    public UserAccess()
    {
        AccessEntries = new Dictionary<EnumAccessLevel, List<AccessEntry>>
        {
            { EnumAccessLevel.VOICE, new List<AccessEntry>() },
            { EnumAccessLevel.DENY, new List<AccessEntry>() }
        };
    }
    
    public override bool CanModifyAccessLevel(IChatObject source,
        IChatObject target,
        EnumAccessLevel accessLevel)
    {
        return source == target;
    }
    
    public override bool CanModifyAccessEntry(IChatObject source,
        IChatObject target,
        AccessEntry entry)
    {
        return source == target;
    }
}