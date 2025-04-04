using Irc.Commands;
using Irc.Enumerations;
using Irc.Extensions.Access.Server;
using Irc.Extensions.Commands;
using Irc.Extensions.Factories;
using Irc.Extensions.Interfaces;
using Irc.Extensions.Objects.Channel;
using Irc.Extensions.Objects.User;
using Irc.Extensions.Protocols;
using Irc.Extensions.Security;
using Irc.Extensions.Security.Credentials;
using Irc.Factories;
using Irc.Interfaces;
using Irc.IO;
using Irc.Objects;
using Irc.Objects.Server;
using Irc.Security;
using Irc7d;

namespace Irc.Extensions.Objects.Server;

public class ExtendedServer : global::Irc.Objects.Server.Server, IServer, IExtendedChatObject, IExtendedServerObject
{
    public ExtendedServer(ISocketServer socketServer, ISecurityManager securityManager,
        IFloodProtectionManager floodProtectionManager, IDataStore dataStore, IList<IChannel> channels,
        IUserFactory userFactory = null, ICredentialProvider? credentialProvider = null) :
        base(socketServer, securityManager,
            floodProtectionManager, dataStore, channels, userFactory ?? new ExtendedUserFactory())
    {
        if (SupportPackages.Contains("NTLM"))
            GetSecurityManager()
                .AddSupportPackage(new Security.Packages.NTLM(credentialProvider ?? new NtlmProvider()));

        AddProtocol(EnumProtocolType.IRCX, new IrcX());
        AddCommand(new Auth());
        AddCommand(new AuthX());
        AddCommand(new Ircx());
        AddCommand(new Prop());
        AddCommand(new Listx());

        var modes = new ExtendedChannelModes().GetSupportedModes();
        modes = new string(modes.OrderBy(c => c).ToArray());
        _dataStore.Set("supported.channel.modes", modes);
        _dataStore.Set("supported.user.modes", new ExtendedUserModes().GetSupportedModes());
    }

    public IPropCollection PropCollection { get; }
    public IAccessList AccessList { get; } = new ServerAccess();

    public virtual void ProcessCookie(IUser user, string name, string value)
    {
        // IRCX Does not use this
    }

    public override IChannel CreateChannel(string name)
    {
        return new ExtendedChannel(name, new ExtendedChannelModes(), new DataStore(name, "store"));
    }

    public override IChannel CreateChannel(IUser creator, string name, string key)
    {
        var channel = (ExtendedChannel)CreateChannel(name);
        channel.ChannelStore.Set("topic", name);
        channel.PropCollection.GetProp(ExtendedResources.ChannelPropOwnerkey).SetValue(key);
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