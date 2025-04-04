using System.Text;
using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Extensions.Apollo.Factories;
using Irc.Extensions.Apollo.Objects.Channel;
using Irc.Extensions.Apollo.Objects.User;
using Irc.Extensions.Apollo.Protocols;
using Irc.Extensions.Apollo.Security.Credentials;
using Irc.Extensions.Apollo.Security.Packages;
using Irc.Extensions.Apollo.Security.Passport;
using Irc.Extensions.Commands;
using Irc.Extensions.Objects.Server;
using Irc.Extensions.Security;
using Irc.Extensions.Security.Packages;
using Irc.Factories;
using Irc.Interfaces;
using Irc.IO;
using Irc.Objects;
using Irc.Security;
using Irc7d;

namespace Irc.Extensions.Apollo.Objects.Server;

public class ApolloServer : ExtendedServer
{
    private readonly PassportV4 passport;

    public ApolloServer(ISocketServer socketServer, ISecurityManager securityManager,
        IFloodProtectionManager floodProtectionManager, IDataStore dataStore, IList<IChannel> channels,
        IUserFactory userFactory = null,
        ICredentialProvider? ntlmCredentialProvider = null)
        : base(socketServer, securityManager,
            floodProtectionManager, dataStore, channels, userFactory ?? new ApolloUserFactory(),
            ntlmCredentialProvider)
    {
        if (SupportPackages.Contains("GateKeeper"))
        {
            passport = new PassportV4(dataStore.Get("Passport.V4.AppID"), dataStore.Get("Passport.V4.Secret"));
            securityManager.AddSupportPackage(new GateKeeper());
            securityManager.AddSupportPackage(new GateKeeperPassport(new PassportProvider(passport)));
        }

        AddProtocol(EnumProtocolType.IRC3, new Irc3());
        AddProtocol(EnumProtocolType.IRC4, new Irc4());
        AddProtocol(EnumProtocolType.IRC5, new Irc5());
        AddProtocol(EnumProtocolType.IRC6, new Irc6());
        AddProtocol(EnumProtocolType.IRC7, new Irc7());
        AddProtocol(EnumProtocolType.IRC8, new Irc8());

        // Override by adding command support at base IRC
        AddCommand(new Auth());
        AddCommand(new AuthX());
        AddCommand(new Ircvers());
        AddCommand(new Prop());

        var modes = new ApolloChannelModes().GetSupportedModes();
        modes = new string(modes.OrderBy(c => c).ToArray());

        _dataStore.Set("supported.channel.modes", modes);
        _dataStore.Set("supported.user.modes", new ApolloUserModes().GetSupportedModes());
    }

    public override IChannel CreateChannel(string name)
    {
        return new ApolloChannel(name, new ApolloChannelModes(), new DataStore(name, "store"));
    }

    public override void ProcessCookie(IUser user, string name, string value)
    {
        if (name == Resources.UserPropMsnRegCookie && user.IsAuthenticated() && !user.IsRegistered())
        {
            var nickname = passport.ValidateRegCookie(value);
            if (nickname != null)
            {
                var encodedNickname = Encoding.Latin1.GetString(Encoding.UTF8.GetBytes(nickname));
                user.Nickname = encodedNickname;

                // Set the RealName to empty string to allow it to pass register
                user.GetAddress().RealName = string.Empty;
            }
        }
        else if (name == Resources.UserPropSubscriberInfo && user.IsAuthenticated() && user.IsRegistered())
        {
            var subscribedString =
                passport.ValidateSubscriberInfo(value, user.GetSupportPackage().GetCredentials().GetIssuedAt());
            int.TryParse(subscribedString, out var subscribed);
            if ((subscribed & 1) == 1) ((ApolloUser)user).GetProfile().Registered = true;
        }
        else if (name == Resources.UserPropMsnProfile && user.IsAuthenticated() && !user.IsRegistered())
        {
            int.TryParse(value, out var profileCode);
            ((ApolloUser)user).GetProfile().SetProfileCode(profileCode);
        }
        else if (name == Resources.UserPropRole && user.IsAuthenticated())
        {
            var dict = passport.ValidateRole(value);
            if (dict == null) return;

            if (dict.ContainsKey("umode"))
            {
                var modes = dict["umode"];
                foreach (var mode in modes)
                {
                    var modeRule = user.GetModes().GetMode(mode);
                    modeRule?.Set(1);
                    modeRule?.DispatchModeChange((ChatObject)user, (ChatObject)user, true, string.Empty);
                }
            }

            if (dict.ContainsKey("utype"))
            {
                var levelType = dict["utype"];

                switch (levelType)
                {
                    case "A":
                    {
                        user.ChangeNickname(user.Nickname, true);
                        user.PromoteToAdministrator();
                        break;
                    }
                    case "S":
                    {
                        user.ChangeNickname(user.Nickname, true);
                        user.PromoteToSysop();
                        break;
                    }
                    case "G":
                    {
                        user.ChangeNickname(user.Nickname, true);
                        user.PromoteToGuide();
                        break;
                    }
                }
            }
        }
    }
}