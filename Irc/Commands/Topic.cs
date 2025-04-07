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
        var channelName = chatFrame.Message.Parameters.First();
        var topic = chatFrame.Message.Parameters[1];

        if (chatFrame.Message.Parameters.Count > 2) topic = chatFrame.Message.Parameters[2];

        var channel = chatFrame.Server.GetChannelByName(channelName);
        if (channel == null)
        {
            chatFrame.User.Send(Raw.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User,
                chatFrame.Message.Parameters.First()));
        }
        else
        {
            var result = ProcessTopic(chatFrame, channel, source, topic);
            switch (result)
            {
                case EnumIrcError.ERR_NOTONCHANNEL:
                {
                    chatFrame.User.Send(Raw.IRCX_ERR_NOTONCHANNEL_442(chatFrame.Server, source, channel));
                    break;
                }
                case EnumIrcError.ERR_NOCHANOP:
                {
                    chatFrame.User.Send(
                        Raw.IRCX_ERR_CHANOPRIVSNEEDED_482(chatFrame.Server, source, channel));
                    break;
                }
                case EnumIrcError.OK:
                {
                    channel.Send(Raw.RPL_TOPIC_IRC(chatFrame.Server, source, channel, topic));
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
            chatFrame.User.Send(Raw.IRCX_ERR_NOTONCHANNEL_442(chatFrame.Server, source, channel));
            return EnumIrcError.ERR_NOTONCHANNEL;
        }

        if (sourceMember.GetLevel() < EnumChannelAccessLevel.ChatHost && channel.Modes.TopicOp)
            return EnumIrcError.ERR_NOCHANOP;

        channel.ChannelStore.Set("topic", topic);
        return EnumIrcError.OK;
    }
}