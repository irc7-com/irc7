using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc.Commands;

internal class Topic : Command, ICommand
{
    public Topic() : base(2)
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
        var topic = chatFrame.ChatMessage.Parameters[1];

        if (chatFrame.ChatMessage.Parameters.Count > 2) topic = chatFrame.ChatMessage.Parameters[2];

        var channel = chatFrame.Server.GetChannelByName(channelName);
        if (channel == null)
        {
            chatFrame.User.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User,
                chatFrame.ChatMessage.Parameters.First()));
        }
        else
        {
            var result = ProcessTopic(chatFrame, channel, source, topic);
            switch (result)
            {
                case EnumIrcError.ERR_NOTONCHANNEL:
                {
                    chatFrame.User.Send(Raws.IRCX_ERR_NOTONCHANNEL_442(chatFrame.Server, source, channel));
                    break;
                }
                case EnumIrcError.ERR_NOCHANOP:
                {
                    chatFrame.User.Send(
                        Raws.IRCX_ERR_CHANOPRIVSNEEDED_482(chatFrame.Server, source, channel));
                    break;
                }
                case EnumIrcError.OK:
                {
                    channel.Send(Raws.RPL_TOPIC_IRC(chatFrame.Server, source, channel, topic));
                    break;
                }
            }
        }
    }

    public static EnumIrcError ProcessTopic(IChatFrame chatFrame, IChannel channel, IUser source, string topic)
    {
        var sourceMember = channel.GetMember(source);
        if (sourceMember == null || !channel.CanBeModifiedBy((ChatObject)source))
        {
            chatFrame.User.Send(Raws.IRCX_ERR_NOTONCHANNEL_442(chatFrame.Server, source, channel));
            return EnumIrcError.ERR_NOTONCHANNEL;
        }

        if (sourceMember.GetLevel() < EnumChannelAccessLevel.ChatHost && channel.Modes.TopicOp)
            return EnumIrcError.ERR_NOCHANOP;

        channel.Props.Topic.Value = topic;
        return EnumIrcError.OK;
    }
}