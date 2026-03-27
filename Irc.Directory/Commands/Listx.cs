using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;

namespace Irc.Directory.Commands;

/// <summary>
/// ADS-side LISTX command (doc section 2.1.1).
///
/// Enumerates channels from the ChannelStore (populated by CHANNEL-UPDATE
/// messages from the ChannelMaster BroadcastProcess).
///
/// Only supports filters for which data is available in ChannelStoreEntry:
///   - Member count: &lt;# (less than), &gt;# (greater than)
///   - Name mask: N=&lt;mask&gt;
///   - Query limit: integer
///
/// Filters that require data not present in ChannelStoreEntry (topic, subject,
/// language, creation time, registration, etc.) are silently ignored.
///
/// When the ChannelStore is not available (no Redis), returns an empty list.
/// </summary>
internal class Listx : Command, ICommand
{
    public Listx()
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = (DirectoryServer)chatFrame.Server;
        var user = chatFrame.User;
        var parameters = chatFrame.ChatMessage.Parameters;

        // If no ChannelStore (no Redis), return an empty list
        if (server.ChannelStore == null)
        {
            user.Send(Raws.IRCX_RPL_LISTXSTART_811(server, user));
            user.Send(Raws.IRCX_RPL_LISTXEND_817(server, user));
            return;
        }

        var allChannels = server.ChannelStore.GetAllChannels();
        var firstParam = parameters.FirstOrDefault();
        var (result, truncated) = FilterChannels(allChannels, firstParam);

        SendListxResponse(server, user, result, truncated);
    }

    /// <summary>
    /// Filters and orders a list of ChannelStoreEntry objects based on LISTX query parameters.
    /// Returns the filtered list and whether it was truncated.
    /// </summary>
    public static (List<ChannelStoreEntry> channels, bool truncated) FilterChannels(
        IReadOnlyList<ChannelStoreEntry> allChannels, string? filterParam)
    {
        var channels = (IEnumerable<ChannelStoreEntry>)allChannels;
        int queryLimit = 0;
        bool hasLimit = false;

        if (filterParam != null)
        {
            var queryTerms = Tools.CSVToArray(filterParam);
            foreach (var term in queryTerms)
            {
                if (term.StartsWith("<") && int.TryParse(term.Substring(1), out var lessThan))
                {
                    channels = channels.Where(c => c.MemberCount < lessThan);
                }
                else if (term.StartsWith(">") && int.TryParse(term.Substring(1), out var greaterThan))
                {
                    channels = channels.Where(c => c.MemberCount > greaterThan);
                }
                else if (term.StartsWith("N="))
                {
                    var mask = term.Substring(2);
                    channels = channels.Where(c => Tools.MatchesMask(c.ChannelName, mask));
                }
                else if (int.TryParse(term, out var limit))
                {
                    queryLimit = limit;
                    hasLimit = true;
                }
                // Unsupported filters (C<, C>, T<, T>, T=, S=, L=, R=, B=, X=, P=)
                // are silently ignored — the ADS doesn't have this data.
            }
        }

        // Materialize the filtered list with deterministic ordering (by name)
        var result = channels.OrderBy(c => c.ChannelName, StringComparer.OrdinalIgnoreCase).ToList();

        // Apply truncation
        bool truncated = false;
        if (hasLimit && queryLimit > 0 && result.Count > queryLimit)
        {
            result = result.Take(queryLimit).ToList();
            truncated = true;
        }

        return (result, truncated);
    }

    /// <summary>
    /// Sends the LISTX response to the user with the given filtered channels.
    /// </summary>
    public static void SendListxResponse(IServer server, IUser user,
        List<ChannelStoreEntry> channels, bool truncated)
    {
        user.Send(Raws.IRCX_RPL_LISTXSTART_811(server, user));

        foreach (var entry in channels)
        {
            // ADS doesn't have modes, member limit, or topic — send defaults
            user.Send(Raws.IRCX_RPL_LISTXLIST_812(
                server,
                user,
                entry.ChannelName,
                "+", // no mode data available
                entry.MemberCount,
                0,   // no member limit data available
                ""   // no topic data available
            ));
        }

        if (truncated)
        {
            user.Send(Raws.IRCX_RPL_LISTXTRUNC_816(server, user));
        }

        user.Send(Raws.IRCX_RPL_LISTXEND_817(server, user));
    }
}
