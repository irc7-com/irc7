using Irc;
using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;

public class Nick : Command, ICommand
{
    public Nick() : base(1, false)
    {
    }

    public EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Standard;
    }

    public void Execute(IChatFrame chatFrame)
    {
        var hopcount = string.Empty;
        if (chatFrame.Message.Parameters.Count > 1) hopcount = chatFrame.Message.Parameters[1];

        // Is user not registered?
        // Set nickname according to regulations (should be available in user object and changes based on what they authenticated as)
        HandlePreauthNicknameChange(chatFrame);
    }

    public static bool ValidateNickname(string nickname)
    {
        var mask = Resources.PreAuthNicknameMask;

        return nickname.Length <= Resources.MaxFieldLen &&
               RegularExpressions.Match(mask, nickname, true);
    }

    public static bool HandlePreauthNicknameChange(IChatFrame chatFrame)
    {
        var nickname = chatFrame.Message.Parameters.First();
        // UTF8 / Guest / Normal / Admin/Sysop/Guide OK
        if (!ValidateNickname(nickname))
        {
            chatFrame.User.Send(Raw.IRCX_ERR_ERRONEOUSNICK_432(chatFrame.Server, chatFrame.User, nickname));
            return false;
        }

        chatFrame.User.Nickname = nickname;
        return true;
    }
}