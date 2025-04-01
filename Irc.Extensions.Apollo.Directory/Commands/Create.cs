using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Extensions.Apollo.Directory.Commands;

internal class Create : Command, ICommand
{
    private bool _isAds;

    public Create(bool isAds = false)
    {
        _requiredMinimumParameters = 1;
        _isAds = isAds;
    }

    public EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public void Execute(IChatFrame chatFrame)
    {
        var messageToSend = Raw.IRCX_RPL_FINDS_613(chatFrame.Server, chatFrame.User);
        if (_isAds)
        {
            messageToSend = ApolloDirectoryRaws.RPL_FINDS_MSN((DirectoryServer)chatFrame.Server, chatFrame.User);
        }

        chatFrame.User.Send(messageToSend);
    }
}