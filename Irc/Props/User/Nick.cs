using Irc.Constants;
using Irc.Interfaces;

namespace Irc.Props.User;

internal class Nick : PropRule, IPropRule
{
    private readonly IDataStore dataStore;

    // limited to 200 bytes including 1 or 2 characters for channel prefix
    public Nick(IDataStore dataStore) : base(ExtendedResources.UserPropNickname, EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.None, Resources.GenericProps, string.Empty, true)
    {
        this.dataStore = dataStore;
    }

    public override string GetValue(IChatObject target)
    {
        return dataStore.Get("Name");
    }
}