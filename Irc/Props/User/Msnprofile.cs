using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.User;

namespace Irc.Props.User;

public class Msnprofile : PropRule
{
    public Msnprofile() : base(ExtendedResources.UserPropMsnProfile, EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.ChatMember, Resources.GenericProps, "0", true)
    {
    }

    public override EnumIrcError EvaluateSet(IChatObject source, IChatObject target, string propValue)
    {
        if (source != target) return EnumIrcError.ERR_NOPERMS;

        var user = (ApolloUser)source;
        if (int.TryParse(propValue, out var result))
        {
            var profile = user.GetProfile();
            if (profile.HasProfile)
            {
                user.Send(Raw.IRCX_ERR_ALREADYREGISTERED_462(user.Server, user));
                return EnumIrcError.OK;
            }

            profile.SetProfileCode(result);
            return EnumIrcError.OK;
        }

        return EnumIrcError.ERR_BADVALUE;
    }
}