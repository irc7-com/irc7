using Irc.Commands;
using Irc.Constants;
using Irc.Directory;
using Irc.Enumerations;
using Irc.Interfaces;

internal class Create : Command, ICommand
{
    private readonly bool _isAds;

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
        var messageToSend = Raws.IRCX_RPL_FINDS_613(chatFrame.Server, chatFrame.User);
        if (_isAds)
            messageToSend = DirectoryRaws.RPL_FINDS_MSN((DirectoryServer)chatFrame.Server, chatFrame.User);

        chatFrame.User.Send(messageToSend);
    }
}