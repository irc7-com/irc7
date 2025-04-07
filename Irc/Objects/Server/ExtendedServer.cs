using Irc.Access.Server;
using Irc.Commands;
using Irc.Enumerations;
using Irc.Factories;
using Irc.Interfaces;
using Irc.IO;
using Irc.Objects.Channel;
using Irc.Objects.Collections;
using Irc.Objects.User;
using Irc.Security.Credentials;
using Irc.Security.Packages;

namespace Irc.Objects.Server;

public class ExtendedServer : Server, IServer, IExtendedChatObject, IExtendedServerObject
{
    private readonly ICredentialProvider? _credentialProvider;

    public ExtendedServer(ISocketServer socketServer, ISecurityManager securityManager,
        IFloodProtectionManager floodProtectionManager, IDataStore dataStore, IList<IChannel> channels,
        ICredentialProvider? credentialProvider = null) :
        base(socketServer, securityManager,
            floodProtectionManager, dataStore, channels)
    {
        _credentialProvider = credentialProvider;
        PropCollection = new PropCollection();
        UserFactory = new ExtendedUserFactory();

        if (SupportPackages.Contains("NTLM"))
            GetSecurityManager()
                .AddSupportPackage(new NTLM(credentialProvider ?? new NtlmProvider()));

        AddProtocol(EnumProtocolType.IRCX, new IrcX());
        AddCommand(new Auth());
        AddCommand(new AuthX());
        AddCommand(new Ircx());
        AddCommand(new Prop());
        AddCommand(new Listx());

        var modes = new ExtendedChannelModes().GetSupportedModes();
        modes = new string(modes.OrderBy(c => c).ToArray());
        _DataStore.Set("supported.channel.modes", modes);
        _DataStore.Set("supported.user.modes", new ExtendedUserModes().GetSupportedModes());
    }

    public IPropCollection PropCollection { get; }
    public IAccessList AccessList { get; } = new ServerAccess();

    public virtual void ProcessCookie(IUser user, string name, string value)
    {
        // IRCX Does not use this
    }

    public new ICredentialProvider? GetCredentialManager()
    {
        return _credentialProvider;
    }

    public override IChannel CreateChannel(string name)
    {
        return new ExtendedChannel(name, new ExtendedChannelModes(), new DataStore(name, "store"));
    }

    public override IChannel CreateChannel(IUser creator, string name, string key)
    {
        var channel = (ExtendedChannel)CreateChannel(name);
        channel.ChannelStore.Set("topic", name);
        var ownerkeyProp = channel.PropCollection.GetProp(ExtendedResources.ChannelPropOwnerkey);
        ownerkeyProp?.SetValue(key);
        channel.Modes.NoExtern = true;
        channel.Modes.TopicOp = true;
        channel.Modes.UserLimit = 50;
        AddChannel(channel);
        return channel;
    }

    // Ircx
    protected EnumChannelAccessResult CheckAuthOnly()
    {
        if (Modes.GetModeChar(ExtendedResources.ChannelModeAuthOnly) == 1)
            return EnumChannelAccessResult.ERR_AUTHONLYCHAN;
        return EnumChannelAccessResult.NONE;
    }

    protected EnumChannelAccessResult CheckSecureOnly()
    {
        // TODO: Whatever this is...
        return EnumChannelAccessResult.ERR_SECUREONLYCHAN;
    }
}