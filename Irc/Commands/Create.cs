using Irc.Constants;
using Irc.Enumerations;
using Irc.Infrastructure;
using Irc.Interfaces;
using Irc.Objects.Channel;

namespace Irc.Commands;

public class Create : Command, ICommand
{
    public Create()
    {
        _requiredMinimumParameters = 8;
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {

        var channel = ProcessCreateRequest(chatFrame);
        var createdChannel = chatFrame.Server.CreateChannel(channel.ChannelName, channel.ChannelTopic, channel.OwnerKey);
        
        if (createdChannel == null)
        {
            // Another server claimed it first
            chatFrame.User.Send(
                Raws.IRCX_RPL_FINDS_CHANNELEXISTS_705(chatFrame.Server, chatFrame.User)
            );
            return;
        }
        
        InMemoryChannelRepository.Add(channel);
        chatFrame.User.Send(Raws.IRCX_RPL_FINDS_613(chatFrame.Server, chatFrame.User));
    }

    public static InMemoryChannel? ProcessCreateRequest(IChatFrame chatFrame)
    {
        var parameters = new Queue<string>(chatFrame.ChatMessage.Parameters);
        
        // The category code of the created channel. Obtain a list of valid categories using the /listc command.
        var category = parameters.Dequeue();
        if (!Channel.IsAllowedCategory(category))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_FINDS_NOSUCHCAT_701(chatFrame.Server, chatFrame.User)
            );
            return null;
        }

        // The name of the channel. The client will add the %# to the channel name for CREATE.
        // The channel name is limited to 200 bytes.
        var channelName = parameters.Dequeue();
        if (!Channel.ValidName(channelName))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_FINDS_INVALIDCHANNEL_706(chatFrame.Server, chatFrame.User)
            );
            return null;
        }
        
        // The topic of the channel
        var channelTopic = parameters.Dequeue();

        // Initial channel modes, not separated by spaces
        var modes = parameters.Dequeue();
        if (!Channel.IsModeSupported(chatFrame.Server, modes))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_FINDS_INVALIDMODE_706(chatFrame.Server, chatFrame.User)
            );
            return null;
        }

        // Optional limit param if specified in modes
        // Default to 50
        ushort limit = 50;
        if (modes.Contains('l'))
        {
            ushort.TryParse(parameters.Dequeue(), out var userDefinedLimit);
            if (userDefinedLimit == 0)
            {
                chatFrame.User.Send(
                    Raws.IRCX_RPL_FINDS_INVALIDLIMIT_706(chatFrame.Server, chatFrame.User)
                );
                return null;
            }
            limit = userDefinedLimit;
        }
        
        // As per apollo docs
        /*
         * - (hyphen) No modes set.
         * Default modes n (no external messages), t (topic may be set by hosts and owners only) and l (user limit) 25.
         */
        // However the default limit of 25 raised to 50 at some point
        if (modes == "-") modes = "ntl";

        /*
         * Locale is the preferred language for the channel.
         * The language property must be 5 characters long with the middle character being a “-“ (hyphen).
         * The locale is not case sensitive.
         */
        var locale = parameters.Dequeue();
        if (!Channel.IsAllowedLocale(locale))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_CREATE_INVALIDLOCALE_706(chatFrame.Server, chatFrame.User)
            );
            return null;
        }
        
        // A value from 1 to 24
        int.TryParse(parameters.Dequeue(), out var language);
        if (!Channel.IsAllowedLanguage(language))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_CREATE_INVALIDLANGUAGE_706(chatFrame.Server, chatFrame.User)
            );
            return null;
        }
        
        var ownerkey = parameters.Dequeue();
        if (!Channel.IsSupportedKey(ownerkey))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_CREATE_INVALIDKEY_706(chatFrame.Server, chatFrame.User)
            );
            return null;
        }

        // Legacy Radio Station ID
        if (!int.TryParse(parameters.Dequeue(), out var legacyRadioStationId) || legacyRadioStationId != 0)
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_CREATE_BADLYFORMED_706(chatFrame.Server, chatFrame.User)
            );
            return null;
        }

        var hostkey = string.Empty;
        if (parameters.Count > 0) hostkey = parameters.Dequeue();
        
        return new InMemoryChannel
        {
            Category = category,
            ChannelName = channelName,
            ChannelTopic = channelTopic,
            Modes = modes,
            UserLimit = limit,
            Locale = locale,
            Language = Convert.ToInt32(language),
            OwnerKey = ownerkey,
            HostKey = hostkey,
            LRSID = legacyRadioStationId,
        };
    }


}

// /CREATE GN %#test %An\bamazing\btopic - EN-US 1 62269 0
