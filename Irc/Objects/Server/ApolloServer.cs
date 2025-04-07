using System.Text;
using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Factories;
using Irc.Interfaces;
using Irc.IO;
using Irc.Modes;
using Irc.Objects.Channel;
using Irc.Objects.User;
using Irc.Protocols;
using Irc.Security.Credentials;
using Irc.Security.Packages;
using Irc.Security.Passport;

namespace Irc.Objects.Server;

public class ApolloServer : ExtendedServer
{
    private readonly PassportV4 _passport = new(string.Empty, string.Empty);

    public ApolloServer(ISocketServer socketServer, ISecurityManager securityManager,
        IFloodProtectionManager floodProtectionManager, IDataStore dataStore, IList<IChannel> channels,
        ICredentialProvider? ntlmCredentialProvider = null)
        : base(socketServer, securityManager,
            floodProtectionManager, dataStore, channels,
            ntlmCredentialProvider)
    {
        UserFactory = new ApolloUserFactory();

        if (SupportPackages.Contains("GateKeeper"))
        {
            _passport = new PassportV4(dataStore.Get("Passport.V4.AppID"), dataStore.Get("Passport.V4.Secret"));
            securityManager.AddSupportPackage(new GateKeeper(new DefaultProvider()));
            securityManager.AddSupportPackage(new GateKeeperPassport(new PassportProvider(_passport)));
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

        _DataStore.Set("supported.channel.modes", modes);
        _DataStore.Set("supported.user.modes", new ApolloUserModes().GetSupportedModes());
    }

    public override IChannel CreateChannel(string name)
    {
        return new ApolloChannel(name, new ApolloChannelModes(), new DataStore(name, "store"));
    }

    public override void ProcessCookie(IUser user, string name, string value)
    {
        if (name == Resources.UserPropMsnRegCookie && user.IsAuthenticated() && !user.IsRegistered())
        {
            var nickname = _passport.ValidateRegCookie(value);
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
            var issuedAt = user.GetSupportPackage()?.GetCredentials()?.GetIssuedAt();
            if (!issuedAt.HasValue) return;

            var subscribedString =
                _passport.ValidateSubscriberInfo(value, issuedAt.Value);
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
            var dict = _passport.ValidateRole(value);
            if (dict == null) return;

            if (dict.ContainsKey("umode"))
            {
                var modes = dict["umode"];
                foreach (var mode in modes)
                {
                    var userModes = (UserModes)user.GetModes();
                    if (userModes.HasMode(mode)) userModes[mode].Set(true);
                    ModeRule.DispatchModeChange(mode, (IChatObject)user, (IChatObject)user, true, string.Empty);
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