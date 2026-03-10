using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;

namespace Irc.Commands;

public class Join : Command, ICommand
{
    public Join() : base(1)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var server = chatFrame.Server;
        var user = chatFrame.User;
        var channels = chatFrame.ChatMessage.Parameters.First();
        var key = chatFrame.ChatMessage.Parameters.Count > 1 ? chatFrame.ChatMessage.Parameters[1] : string.Empty;

        var channelNames = ValidateChannels(server, user, channels);
        if (channelNames.Count == 0) return;

        JoinChannels(server, user, channelNames, key);
    }

    public static List<string> ValidateChannels(IServer server, IUser user, string channels)
    {
        var channelNames = channels.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        if (channelNames.Count == 0)
        {
            user.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(server, user, string.Empty));
        }
        else
        {
            var invalidChannelNames = channelNames.Where(c => !Channel.ValidName(c)).ToList();
            channelNames.RemoveAll(c => invalidChannelNames.Contains(c));

            // TODO: Could do better below for reporting invalid channel / empty channel
            if (invalidChannelNames.Count > 0)
                invalidChannelNames.ForEach(c => user.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(server, user, c)));
        }

        return channelNames;
    }

    public static void JoinChannels(IServer server, IUser user, List<string> channelNames, string key)
    {
        // TODO: Optimize the below code
        foreach (var channelName in channelNames)
        {
            var isCreator = false;
            
            if (user.GetChannels().Count >= server.MaxChannels)
            {
                user.Send(Raws.IRCX_ERR_TOOMANYCHANNELS_405(server, user, channelName));
                continue;
            }

            var channel = server
                .GetChannelByName(channelName);
            
            if (channel == null)
            {
                if (!server.JoinOnCreate)
                {
                    user.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(server, user, channelName));
                    continue;
                }
                
                channel = server.CreateChannel(channelName, channelName, string.Empty);
                if (channel == null)
                {
                    user.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(server, user, channelName));
                    continue;
                }

                isCreator = true;
            }

            if (channel.HasUser(user))
            {
                user.Send(Raws.IRCX_ERR_ALREADYONCHANNEL_927(server, user, channel));
                continue;
            }

            var channelAccessResult = channel.GetAccess(user, key);
            if (channelAccessResult < EnumChannelAccessResult.SUCCESS_GUEST)
            {
                // Per draft-pfenning-irc-extensions-04 section 8.1.16:
                // When a CLONEABLE channel is full, redirect the user to a clone channel.
                if (channelAccessResult == EnumChannelAccessResult.ERR_CHANNELISFULL &&
                    channel.Modes.Cloneable.ModeValue)
                {
                    if (TryJoinOrCreateClone(server, user, channel, key))
                        continue;

                    // All 99 clone slots are full – inform the client the channel is full.
                    user.Send(Raws.IRCX_ERR_CHANNELISFULL_471(server, user, channel));
                    continue;
                }

                SendJoinError(server, channel, user, channelAccessResult);
                continue;
            }

            channel.Join(user, isCreator ? EnumChannelAccessResult.SUCCESS_OWNER : channelAccessResult)
                .SendTopic(user)
                .SendNames(user)
                .SendOnJoinMessage(user);
        }
    }

    /// <summary>
    /// Attempts to join the user to an existing non-full clone of the parent channel, or creates
    /// a new clone channel (suffix 1-99) if no suitable clone exists.
    /// The first slot tried is the parent channel itself (no numeric suffix); subsequent slots
    /// use numeric suffixes 1-99 (e.g. #chat, #chat1, #chat2, ..., #chat99).
    /// Returns true if the user was successfully joined to a clone, false otherwise.
    /// </summary>
    private static bool TryJoinOrCreateClone(IServer server, IUser user, IChannel parent, string key)
    {
        // i=0  → no suffix (the parent channel itself is the first slot; it will be full, so we skip it)
        // i=1–99 → numeric suffixes 1–99 (the actual numbered clone channels)
        for (var i = 0; i <= 99; i++)
        {
            var cloneName = i == 0 ? parent.GetName() : parent.GetName() + i;
            var existingClone = server.GetChannelByName(cloneName);

            if (existingClone != null)
            {
                if (existingClone.HasUser(user))
                {
                    user.Send(Raws.IRCX_ERR_ALREADYONCHANNEL_927(server, user, existingClone));
                    return true;
                }

                var cloneAccess = existingClone.GetAccess(user, key);
                if (cloneAccess == EnumChannelAccessResult.ERR_CHANNELISFULL)
                    continue; // this clone is full, try the next suffix

                if (cloneAccess < EnumChannelAccessResult.SUCCESS_GUEST)
                {
                    SendJoinError(server, existingClone, user, cloneAccess);
                    return true;
                }

                existingClone.Join(user, cloneAccess)
                    .SendTopic(user)
                    .SendNames(user)
                    .SendOnJoinMessage(user);
                return true;
            }

            // No channel exists with this clone name – create a new one.
            var cloneChannel = CreateCloneChannel(server, parent, cloneName);
            if (cloneChannel == null)
                continue;

            // Notify hosts and owners in the parent channel that a new clone was created.
            parent.Send(Raws.RPL_CLONE(server, parent, cloneChannel), EnumChannelAccessLevel.ChatHost);

            cloneChannel.Join(user, EnumChannelAccessResult.SUCCESS_GUEST)
                .SendTopic(user)
                .SendNames(user)
                .SendOnJoinMessage(user);
            return true;
        }

        return false; // all 99 clone slots are full
    }

    /// <summary>
    /// Creates a clone channel from a parent CLONEABLE channel, inheriting its modes and properties.
    /// The clone channel will have the CLONE mode (+e) set and CLONEABLE (+d) will not be inherited.
    /// </summary>
    private static IChannel? CreateCloneChannel(IServer server, IChannel parent, string cloneName)
    {
        var clone = server.CreateChannel(cloneName);
        if (clone == null) return null;

        // Inherit boolean modes from parent (excluding CLONEABLE; set CLONE instead)
        var parentModes = parent.Modes;
        clone.Modes.NoExtern.ModeValue = parentModes.NoExtern.ModeValue;
        clone.Modes.TopicOp.ModeValue = parentModes.TopicOp.ModeValue;
        clone.Modes.Private.ModeValue = parentModes.Private.ModeValue;
        clone.Modes.Secret.ModeValue = parentModes.Secret.ModeValue;
        clone.Modes.Hidden.ModeValue = parentModes.Hidden.ModeValue;
        clone.Modes.InviteOnly.ModeValue = parentModes.InviteOnly.ModeValue;
        clone.Modes.Moderated.ModeValue = parentModes.Moderated.ModeValue;
        clone.Modes.NoWhisper.ModeValue = parentModes.NoWhisper.ModeValue;
        clone.Modes.NoGuestWhisper.ModeValue = parentModes.NoGuestWhisper.ModeValue;
        clone.Modes.Auditorium.ModeValue = parentModes.Auditorium.ModeValue;
        clone.Modes.AuthOnly.ModeValue = parentModes.AuthOnly.ModeValue;
        clone.Modes.Registered.ModeValue = parentModes.Registered.ModeValue;
        clone.Modes.Knock.ModeValue = parentModes.Knock.ModeValue;

        // Inherit user limit from parent
        clone.Modes.UserLimit.Value = parentModes.UserLimit.Value;

        // Inherit key from parent
        if (parentModes.Key.ModeValue)
        {
            clone.Modes.Key.ModeValue = true;
            clone.Modes.Keypass = parentModes.Keypass;
        }

        // Set CLONE mode to indicate this channel was created by the server as a clone
        clone.Modes.Clone.ModeValue = true;

        // Inherit props from parent
        var parentProps = parent.Props;
        var cloneProps = clone.Props;
        cloneProps.Topic.Value = parentProps.Topic.Value;
        cloneProps.Category.Value = parentProps.Category.Value;
        cloneProps.Language.Value = parentProps.Language.Value;
        cloneProps.Subject.Value = parentProps.Subject.Value;
        cloneProps.OwnerKey.Value = parentProps.OwnerKey.Value;
        cloneProps.HostKey.Value = parentProps.HostKey.Value;
        cloneProps.MemberKey.Value = parentProps.MemberKey.Value;
        cloneProps.Onjoin.Value = parentProps.Onjoin.Value;
        cloneProps.Onpart.Value = parentProps.Onpart.Value;

        if (!server.AddChannel(clone)) return null;

        return clone;
    }

    public static void SendJoinError(IServer server, IChannel channel, IUser user, EnumChannelAccessResult result)
    {
        // Broadcast to channel if Knocks are on
        if (channel.Modes.Knock.ModeValue)
        {
            channel.Send(
                Raws.RPL_KNOCK_CHAN(server, user, channel, result.ToString()), 
                EnumChannelAccessLevel.ChatHost);
        }
        
        // Send error to user
        switch (result)
        {
            case EnumChannelAccessResult.ERR_BADCHANNELKEY:
            {
                user.Send(Raws.IRCX_ERR_BADCHANNELKEY_475(server, user, channel));
                break;
            }
            case EnumChannelAccessResult.ERR_INVITEONLYCHAN:
            {
                user.Send(Raws.IRCX_ERR_INVITEONLYCHAN_473(server, user, channel));
                break;
            }
            case EnumChannelAccessResult.ERR_CHANNELISFULL:
            {
                user.Send(Raws.IRCX_ERR_CHANNELISFULL_471(server, user, channel));
                break;
            }
            default:
            {
                user.Send($"CANNOT JOIN CHANNEL {result.ToString()}");
                break;
            }
        }
    }
}