using System.Text.Json;
using Irc.Contracts.Messages;
using Irc.Objects.Channel;
using NLog;

namespace Irc.Objects.Server;

public static class ServerHandlers
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Handles ASSIGN commands from the ChannelMaster.
    /// Received via cm:cmd:acs:{serverId} pub-sub channel.
    /// Creates the channel locally and writes a response to cm:reply:acs:{requestId}.
    /// </summary>
    public static void HandleChannelMasterAssign(Server server, string payload)
    {
        Log.Trace($"HandleChannelMasterAssign: {payload}");

        AssignRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<AssignRequest>(payload);
        }
        catch (Exception ex)
        {
            Log.Error($"Could not deserialize ASSIGN request: {ex.Message}");
            return;
        }

        if (request == null)
        {
            Log.Error($"ASSIGN request deserialized to null: {payload}");
            return;
        }

        Log.Info($"ASSIGN received: channel={request.ChannelName} uid={request.ChannelUid}");

        // Check if the channel already exists
        if (server.GetChannelByName(request.ChannelName) != null)
        {
            Log.Info($"Channel {request.ChannelName} already exists. Accepting ASSIGN (idempotent).");
            server.CacheManager.WriteAssignResponse(request.RequestId, accepted: true);
            return;
        }

        // Create a minimal channel from the ASSIGN request
        var channel = new Channel.Channel(request.ChannelName);

        if (server.AddChannel(channel))
        {
            Log.Info($"ASSIGN accepted: channel={request.ChannelName} uid={request.ChannelUid}");
            server.CacheManager.WriteAssignResponse(request.RequestId, accepted: true);
        }
        else
        {
            Log.Warn($"ASSIGN rejected: could not add channel {request.ChannelName}");
            server.CacheManager.WriteAssignResponse(request.RequestId, accepted: false);
        }
    }
}