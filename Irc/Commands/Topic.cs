using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc.Commands;

internal class Topic : Command, ICommand
{
    public Topic() : base(1)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Standard;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var source = chatFrame.User;
        var channelName = chatFrame.ChatMessage.Parameters.First();
        
        var channel = chatFrame.Server.GetChannelByName(channelName);
        if (channel == null)
        {
            chatFrame.User.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User,
                chatFrame.ChatMessage.Parameters.First()));
        }
        
        var userIsOnChannel = chatFrame.User.IsOn(channel);

        // If the channel topic is read
        if (chatFrame.ChatMessage.Parameters.Count == 1)
        {
            if (!userIsOnChannel && (channel.Modes.Private.ModeValue || channel.Modes.Secret.ModeValue))
            {
                // Whatever the reply is in this scenario
                chatFrame.User.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User,
                    chatFrame.ChatMessage.Parameters.First()));
                return;
            }

            if (channel.Props.Topic.Value == "")
            {
                chatFrame.User.Send(Raws.IRCX_RPL_NOTOPIC_331(chatFrame.Server, source, channel));
                return;
            }
            
            chatFrame.User.Send(Raws.IRCX_RPL_TOPIC_332(chatFrame.Server, source, channel, channel.Props.Topic.Value));
            return;
        }
        
        var topic = string.Empty;
        if (chatFrame.ChatMessage.Parameters.Count > 1) topic = chatFrame.ChatMessage.Parameters[1];

        ProcessTopic(chatFrame, channel, source, topic);
    }

    public static void ProcessTopic(IChatFrame chatFrame, IChannel channel, IUser source, string topic)
    {
        var sourceMember = channel.GetMember(source);
        if (sourceMember.GetLevel() < EnumChannelAccessLevel.ChatHost && channel.Modes.TopicOp.ModeValue)
        {
            chatFrame.User.Send(
                Raws.IRCX_ERR_CHANOPRIVSNEEDED_482(chatFrame.Server, source, channel));
            return;
        }

        channel.UpdateTopic(topic);
        channel.Send(Raws.RPL_TOPIC_IRC(chatFrame.Server, source, channel, topic));
    }
}