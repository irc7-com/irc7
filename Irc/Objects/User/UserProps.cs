using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Collections;

namespace Irc.Objects.User;

// TODO: Further refactoring of the below to allow us to pass extended logic (e.g. via callbacks)
// Then remove the other classes from this file

public class PropProfile : PropRule
{
    public PropProfile() : base(Resources.UserPropMsnProfile, EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.ChatMember, Resources.GenericProps, "0", true)
    {
    }

    public override EnumIrcError EvaluateSet(IChatObject source, IChatObject target, string propValue)
    {
        if (source != target) return EnumIrcError.ERR_NOPERMS;

        var user = (Objects.User.User)source;
        if (int.TryParse(propValue, out var result))
        {
            var profile = user.GetProfile();
            if (profile.HasProfile)
            {
                user.Send(Raws.IRCX_ERR_ALREADYREGISTERED_462(user.Server, user));
                return EnumIrcError.OK;
            }

            profile.SetProfileCode(result);
            return EnumIrcError.OK;
        }

        return EnumIrcError.ERR_BADVALUE;
    }
}

public class PropNick : PropRule, IPropRule
{
    // limited to 200 bytes including 1 or 2 characters for channel prefix
    public PropNick() : base(Resources.UserPropNickname, EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.None, Resources.GenericProps, string.Empty, true)
    {
    }

    public override string GetValue(IChatObject target)
    {
        var user = (IUser)target;
        return user.GetAddress().Nickname;
    }
}

public class PropPuid : PropRule, IPropRule
{
    // limited to 200 bytes including 1 or 2 characters for channel prefix
    public PropPuid() : base(Resources.UserPropPuid, EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.None, Resources.GenericProps, string.Empty, true)
    {
    }

    public override EnumIrcError EvaluateGet(IChatObject source, IChatObject target)
    {
        if (source is not IUser sourceUser || target is not IUser targetUser)
            return EnumIrcError.ERR_NOPERMS;

        // If target is a guest, deny access
        if (targetUser.IsGuest())
            return EnumIrcError.ERR_NOPERMS;

        // If the target is not passport authenticated, deny access
        var sspiHandler = targetUser.GetSspiHandler();
        if (sspiHandler == null || !sspiHandler.RequiresPassport)
            return EnumIrcError.ERR_NOPERMS;

        // If source and target are not on the same channel, deny access
        var sharedChannel = sourceUser.GetChannels().Keys
            .Any(channel => targetUser.IsOn(channel));

        if (!sharedChannel)
            return EnumIrcError.ERR_NOPERMS;

        // GetValue will return the PUID from the credentials
        return EnumIrcError.OK;
    }

    public override string GetValue(IChatObject target)
    {
        var user = (IUser)target;
        var sspiHandler = user.GetSspiHandler();
        if (sspiHandler == null) return string.Empty;
        
        var credentials = sspiHandler.GetCredentials();
        if (credentials == null) return string.Empty;

        return credentials.Username;
    }
}

public class PropRole : PropRule
{
    public PropRole() : base(Resources.UserPropRole, EnumChannelAccessLevel.None,
        EnumChannelAccessLevel.ChatMember, Resources.GenericProps, "0", true)
    {
    }

    public override EnumIrcError EvaluateSet(IChatObject source, IChatObject target, string propValue)
    {
        var user = (IUser)source;
        user.Server.ProcessCookie(user, Resources.UserPropRole, propValue);
        return EnumIrcError.NONE;
    }
}

public class PropSubInfo : PropRule
{
    public PropSubInfo() : base(Resources.UserPropSubscriberInfo,
        EnumChannelAccessLevel.None, EnumChannelAccessLevel.ChatMember, Resources.GenericProps, "0", true)
    {
    }

    public override EnumIrcError EvaluateSet(IChatObject source, IChatObject target, string propValue)
    {
        var user = (IUser)source;
        user.Server.ProcessCookie(user, Resources.UserPropSubscriberInfo, propValue);
        return EnumIrcError.NONE;
    }
}

public class UserProps : PropCollection, IUserProps
{
    public PropNick Nick { get; } = new();
    public PropSubInfo SubscriberInfo { get; } = new();
    public PropProfile Profile { get; } = new();
    public PropRole Role { get; } = new();
    public PropPuid Puid { get; } = new();
    
    public UserProps()
    {
        // IRC Props
        AddProp(Nick);

        // Apollo Props
        AddProp(SubscriberInfo);
        AddProp(Profile);
        AddProp(Role);
        AddProp(Puid);
    }
}