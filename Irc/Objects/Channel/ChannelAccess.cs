using Irc.Access;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Objects.Channel;

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

    private bool CheckChannelAccess(
        IChatObject source, 
        IChatObject target, 
        EnumAccessLevel accessLevel,
        EnumUserAccessLevel entryLevel)
    {
        if (source is not IUser) return false;
        
        var user = (IUser)source;
        var channel = (IChannel)target;
        
        // Oper path
        //   Opers do not have to be on the channel
        //   User level must be at least as high as the existing user entry level
        if (user.GetLevel() >= EnumUserAccessLevel.Guide) 
            return entryLevel <= user.GetLevel();
        
        // User path
        var member = channel.GetMember(user);
        
        // if not on channel return false
        if (member == null) return false;
        
        var memberLevel = member.GetLevel();
        
        // TODO: At the moment this means a IRCOPs access can be overriden 
        
        return // The member must be at least a host
                memberLevel >= EnumChannelAccessLevel.ChatHost &&
               // It is not that the Access Level is Owner and MemberLevel is less than Owner
               !(accessLevel == EnumAccessLevel.OWNER && memberLevel < EnumChannelAccessLevel.ChatOwner);
    }

    public override bool CanAdd(IChatObject source,
        IChatObject target,
        EnumAccessLevel accessLevel)
    {
        return CheckChannelAccess(source, target, accessLevel, EnumUserAccessLevel.None);
    }
    
    public override bool CanModify(IChatObject source,
        IChatObject target,
        AccessEntry entry)
    {
        return CheckChannelAccess(source, target, entry.AccessLevel, entry.EntryLevel);
    }
}