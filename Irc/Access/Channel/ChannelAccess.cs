using Irc.Enumerations;

namespace Irc.Access.Channel;

public class ChannelAccess : AccessList
{
    public ChannelAccess()
    {
        AccessEntries = new Dictionary<EnumAccessLevel, List<AccessEntry>>
        {
            { EnumAccessLevel.OWNER, new List<AccessEntry>() },
            { EnumAccessLevel.HOST, new List<AccessEntry>() },
            { EnumAccessLevel.VOICE, new List<AccessEntry>() },
            { EnumAccessLevel.DENY, new List<AccessEntry>() }
        };
    }
}