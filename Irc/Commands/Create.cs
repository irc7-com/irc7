using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Infrastructure;
using Irc.Interfaces;
using Irc.Objects.Channel;

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
        if (!IsAllowedCategory(chatFrame.ChatMessage.Parameters[0]))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_FINDS_NOSUCHCAT_701(chatFrame.Server, chatFrame.User)
            );
            return;
        }

        if (!Channel.ValidName(chatFrame.ChatMessage.Parameters[1]))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_FINDS_INVALIDCHANNEL_706(chatFrame.Server, chatFrame.User)
            );
            return;
        }

        if (!IsModeSupported(chatFrame.Server, chatFrame.ChatMessage.Parameters[3]))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_FINDS_INVALIDMODE_706(chatFrame.Server, chatFrame.User)
            );
            return;
        }

        if (!IsAllowedRegion(chatFrame.ChatMessage.Parameters[4]))
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_CREATE_INVALIDREGION_706(chatFrame.Server, chatFrame.User)
            );
            return;
        }

        if (InMemoryChannelRepository.GetByName(chatFrame.ChatMessage.Parameters[1]) != null)
        {
            chatFrame.User.Send(
                Raws.IRCX_RPL_FINDS_CHANNELEXISTS_705(chatFrame.Server, chatFrame.User)
            );
            return;
        }

        int unknownValue;
        int.TryParse(chatFrame.ChatMessage.Parameters[7], out unknownValue);

        var channel = new InMemoryChannel
        {
            Category = chatFrame.ChatMessage.Parameters[0],
            ChannelName = chatFrame.ChatMessage.Parameters[1],
            ChannelTopic = chatFrame.ChatMessage.Parameters[2],
            Modes = chatFrame.ChatMessage.Parameters[3],
            Region = chatFrame.ChatMessage.Parameters[4],
            Language = chatFrame.ChatMessage.Parameters[5],
            OwnerKey = chatFrame.ChatMessage.Parameters[6],
            Unknown = unknownValue,
        };

        var createdChannel = chatFrame.Server.CreateChannel(
            chatFrame.User,
            channel.ChannelName,
            channel.OwnerKey,
            channel.Region,
            channel.Category
        );

        // Add the subject created by the server at creatin time
        if (createdChannel?.Props?.Subject?.Value != null)
        {
            channel.Subject = createdChannel.Props.Subject.Value;
        }

        InMemoryChannelRepository.Add(channel);

        chatFrame.User.Send(Raws.IRCX_RPL_FINDS_613(chatFrame.Server, chatFrame.User));
    }

    private bool IsAllowedCategory(string category) =>
        Resources.SupportedChannelCategories.Contains(category);

    private bool IsAllowedRegion(string region) =>
        Resources.SupportedChannelCountryLanguages.Contains(region);

    private bool IsModeSupported(IServer server, string modes)
    {
        if (modes != "-")
        {
            var supportedModes = server.GetSupportedChannelModes().ToCharArray().ToList();
            var inputModes = modes.ToCharArray().ToList();
            foreach (var mode in inputModes)
            {
                if (mode != 'l' && !supportedModes.Contains(mode))
                {
                    return false;
                }
            }
        }

        return true;
    }
}

// /CREATE GN %#test %An\bamazing\btopic - EN-US 1 62269 0
