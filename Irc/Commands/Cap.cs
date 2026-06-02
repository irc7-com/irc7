using Irc.Constants;
using Irc.Enumerations;
using Irc.Extensions;
using Irc.Interfaces;

namespace Irc.Commands;

/// <summary>
/// Implements the IRCv3 CAP (capability negotiation) command.
/// Spec: https://ircv3.net/specs/extensions/capability-negotiation
/// </summary>
internal class Cap : Command, ICommand
{
    public Cap() : base(1, false)
    {
    }

    public new EnumCommandDataType GetDataType() => EnumCommandDataType.None;

    public new string GetName() => Resources.CommandCap;

    public new void Execute(IChatFrame chatFrame)
    {
        var user = chatFrame.User;
        var server = chatFrame.Server;
        var subCommand = chatFrame.ChatMessage.Parameters[0].ToUpper();

        switch (subCommand)
        {
            case "LS":
                HandleLs(server, user);
                break;
            case "LIST":
                HandleList(server, user);
                break;
            case "REQ":
                HandleReq(server, user, chatFrame);
                break;
            case "END":
                HandleEnd(user);
                break;
            default:
                user.Send(Raws.IRCX_ERR_NEEDMOREPARAMS_461(server, user, Resources.CommandCap));
                break;
        }
    }

    private static void HandleLs(IServer server, IUser user)
    {
        // Mark as negotiating to defer registration until CAP END is received
        user.CapNegotiating = true;

        var caps = string.Join(' ', CapabilityManager.GetSupportedCapabilities());
        user.Send(Raws.CAP_LS(server, user, caps));
    }

    private static void HandleList(IServer server, IUser user)
    {
        var caps = string.Join(' ', user.GetCapabilities());
        user.Send(Raws.CAP_LIST(server, user, caps));
    }

    private static void HandleReq(IServer server, IUser user, IChatFrame chatFrame)
    {
        if (chatFrame.ChatMessage.Parameters.Count < 2)
        {
            user.Send(Raws.IRCX_ERR_NEEDMOREPARAMS_461(server, user, Resources.CommandCap));
            return;
        }

        // The trailing parameter contains the requested capabilities (colon already stripped by parser)
        var requestedCaps = chatFrame.ChatMessage.Parameters[1]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Validate all requested caps before applying any (atomic)
        foreach (var cap in requestedCaps)
        {
            var capName = cap.StartsWith('-') ? cap[1..] : cap;
            if (!CapabilityManager.IsSupported(capName))
            {
                user.Send(Raws.CAP_NAK(server, user, chatFrame.ChatMessage.Parameters[1]));
                return;
            }
        }

        // Apply capability changes
        foreach (var cap in requestedCaps)
        {
            if (cap.StartsWith('-'))
                user.DisableCapability(cap[1..]);
            else
                user.EnableCapability(cap);
        }

        user.Send(Raws.CAP_ACK(server, user, chatFrame.ChatMessage.Parameters[1]));
    }

    private static void HandleEnd(IUser user)
    {
        // End capability negotiation; registration can now proceed
        user.CapNegotiating = false;
    }
}
