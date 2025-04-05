using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Irc.Extensions.Access.Channel
{
    public class ChannelAccess: AccessList
    {
        public ChannelAccess()
        {
            AccessEntries = new Dictionary<Enumerations.EnumAccessLevel, List<AccessEntry>>()
            {
                { Enumerations.EnumAccessLevel.OWNER, new List<AccessEntry>() },
                { Enumerations.EnumAccessLevel.HOST, new List<AccessEntry>() },
                { Enumerations.EnumAccessLevel.VOICE, new List<AccessEntry>() },
                { Enumerations.EnumAccessLevel.DENY, new List<AccessEntry>() },
            };
        }
    }
}
