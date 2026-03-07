using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Access.Server;

public class ServerAccess : AccessList
{
    public ServerAccess()
    {
        AccessEntries = new Dictionary<EnumAccessLevel, List<AccessEntry>>
        {
            { EnumAccessLevel.OWNER, new List<AccessEntry>() },
            { EnumAccessLevel.HOST, new List<AccessEntry>() },
            { EnumAccessLevel.VOICE, new List<AccessEntry>() },
            { EnumAccessLevel.DENY, new List<AccessEntry>() },
            { EnumAccessLevel.GRANT, new List<AccessEntry>() }
        };
    }
    
    public override bool CanModifyAccessLevel(IChatObject source,
        IChatObject target,
        EnumAccessLevel accessLevel) => source is IUser && ((IUser)source).GetLevel() >= EnumUserAccessLevel.Administrator;
    
    public override bool CanModifyAccessEntry(IChatObject source,
        IChatObject target,
        AccessEntry entry) => source is IUser && ((IUser)source).GetLevel() >= EnumUserAccessLevel.Administrator;
}