using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes;
using Irc.Objects;
using Irc.Objects.Channel;

namespace Irc.Commands;

internal class Mode : Command, ICommand
{
    public Mode() : base(1, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        if (!chatFrame.User.IsRegistered())
        {
            if (chatFrame.Message.Parameters.First().ToUpper() == Resources.ISIRCX)
            {
                var protocol = chatFrame.User.GetProtocol().GetProtocolType();
                var isircx = protocol > EnumProtocolType.IRC;
                chatFrame.User.Send(Raw.IRCX_RPL_IRCX_800(chatFrame.Server, chatFrame.User, isircx ? 1 : 0, 0,
                    chatFrame.Server.MaxInputBytes, Resources.IRCXOptions));
            }
        }
        else
        {
            var objectName = chatFrame.Message.Parameters.First();

            ChatObject? chatObject = null;

            // Lookup object
            if (Channel.ValidName(objectName))
                chatObject = (ChatObject?)chatFrame.Server.GetChannelByName(objectName);
            else
                chatObject = (ChatObject?)chatFrame.Server.GetUserByNickname(objectName, chatFrame.User);

            // Execute / List
            if (chatObject == null)
            {
                // :sky-8a15b323126 403 Sky aaa :No such channel
                chatFrame.User.Send(Raw.IRCX_ERR_NOSUCHCHANNEL_403(chatFrame.Server, chatFrame.User, objectName));
                return;
            }

            if (chatFrame.Message.Parameters.Count > 1)
                ProcessModes(chatFrame, chatObject);
            else
                ListModes(chatFrame, chatObject);
        }
    }

    public void ProcessModes(IChatFrame chatFrame, ChatObject chatObject)
    {
        // Perform mode operation
        Queue<string> modeParameters = new();
        if (chatFrame.Message.Parameters.Count > 2)
            modeParameters = new Queue<string>(chatFrame.Message.Parameters.Skip(2).ToArray());
        ModeEngine.Breakdown(chatFrame.User, chatObject, chatFrame.Message.Parameters[1], modeParameters);
    }

    public void ListModes(IChatFrame chatFrame, ChatObject chatObject)
    {
        /*-> sky-8a15b323126 MODE Sky
        <- :sky-8a15b323126 221 Sky +ix
        -> sky-8a15b323126 MODE #test
        <- :sky-8a15b323126 324 Sky #test +tnl 50*/
        if (chatObject is IChannel)
            chatFrame.User.Send(Raw.IRCX_RPL_MODE_324(chatFrame.Server, chatFrame.User, (IChannel)chatObject,
                ((IChannel)chatObject).Modes.ToString()));
        else if (chatObject is IUser)
            chatFrame.User.Send(Raw.IRCX_RPL_UMODEIS_221(chatFrame.Server, chatFrame.User,
                chatObject.Modes.ToString()));
    }
}