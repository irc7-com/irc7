using Irc.Enumerations;

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
}