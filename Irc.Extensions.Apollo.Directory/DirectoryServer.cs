using Irc.Commands;
using Irc.Enumerations;
using Irc.Extensions.Apollo.Commands;
using Irc.Extensions.Apollo.Directory.Commands;
using Irc.Extensions.Apollo.Factories;
using Irc.Extensions.Apollo.Objects.Server;
using Irc.Extensions.Commands;
using Irc.Extensions.Security;
using Irc.Factories;
using Irc.Interfaces;
using Irc.IO;
using Irc.Security;
using Irc7d;
using Nick = Irc.Extensions.Apollo.Directory.Commands.Nick;
using Version = Irc.Commands.Version;

namespace Irc.Extensions.Apollo.Directory;

public class DirectoryServer : ApolloServer
{
    public string ChatServerIP;
    public string ChatServerPORT;

    public DirectoryServer(ISocketServer socketServer, ISecurityManager securityManager,
        IFloodProtectionManager floodProtectionManager, IDataStore dataStore, IList<IChannel> channels,
        ICommandCollection commands, IUserFactory userFactory = null,
        ICredentialProvider? ntlmCredentialProvider = null, string? chatServerIp = null)
        : base(socketServer, securityManager,
            floodProtectionManager, dataStore, channels, userFactory ?? new ApolloUserFactory(),
            ntlmCredentialProvider)
    {
        DisableGuestMode = true;
        DisableUserRegistration = true;

        if (!string.IsNullOrEmpty(chatServerIp))
        {
            var parts = chatServerIp.Split(':');
            if (parts.Length > 0)
                ChatServerIP = parts[0];
            if (parts.Length > 1)
                ChatServerPORT = parts[1];
        }

        FlushCommands();
        AddCommand(new Ircvers());
        AddCommand(new Auth());
        AddCommand(new AuthX());
        AddCommand(new Pass());
        AddCommand(new Nick());
        AddCommand(new UserCommand(), EnumProtocolType.IRC, "User");
        AddCommand(new Finds());
        AddCommand(new Prop());
        AddCommand(new Create(true));
        AddCommand(new Ping());
        AddCommand(new Pong());
        AddCommand(new Version());
        AddCommand(new WebIrc());
    }
}