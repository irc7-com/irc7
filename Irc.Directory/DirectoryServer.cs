using Irc.Commands;
using Irc.Directory.Commands;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Server;
using Nick = Irc.Directory.Commands.Nick;
using Version = Irc.Commands.Version;

namespace Irc.Directory;

public class DirectoryServer : Server
{
    public readonly string ChatServerIp = string.Empty;
    public readonly string ChatServerPort = string.Empty;

    public DirectoryServer(
        ISocketServer socketServer,
        ISecurityManager securityManager,
        IFloodProtectionManager floodProtectionManager,
        IDataStore dataStore,
        IList<IChannel> channels,
        ICredentialProvider? ntlmCredentialProvider = null,
        string? chatServerIp = null,
        string? redisUrl = null
    )
        : base(
            socketServer,
            securityManager,
            floodProtectionManager,
            dataStore,
            channels,
            ntlmCredentialProvider,
            redisUrl
        )
    {
        DisableGuestMode = true;
        DisableUserRegistration = true;
        IsDirectoryServer = true;

        if (!string.IsNullOrEmpty(chatServerIp))
        {
            var parts = chatServerIp.Split(':');
            if (parts.Length > 0)
                ChatServerIp = parts[0];
            if (parts.Length > 1)
                ChatServerPort = parts[1];
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
        AddCommand(new Irc.Directory.Commands.Create(true));
        AddCommand(new Ping());
        AddCommand(new Pong());
        AddCommand(new Version());
        AddCommand(new WebIrc());
    }
}
