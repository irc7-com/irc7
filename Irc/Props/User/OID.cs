using Irc.Constants;
using Irc.Interfaces;

namespace Irc.Props.User;

internal class OID : PropRule
{
    private readonly IDataStore dataStore;

    public OID(IDataStore dataStore) : base(ExtendedResources.UserPropOid, EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.None, Resources.GenericProps, "0", true)
    {
        this.dataStore = dataStore;
    }

    public override string GetValue(IChatObject target)
    {
        return dataStore.Get(Resources.UserPropOid);
    }
}