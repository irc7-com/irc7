using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;

namespace Irc.Directory.Commands;

public class Nick : Command, ICommand
{
    public Nick() : base(1, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.Standard;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var hopcount = string.Empty;
        if (chatFrame.ChatMessage.Parameters.Count > 1) hopcount = chatFrame.ChatMessage.Parameters[1];

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
        var nickname = chatFrame.ChatMessage.Parameters.First();
        // UTF8 / Guest / Normal / Admin/Sysop/Guide OK
        if (!ValidateNickname(nickname))
        {
            chatFrame.User.Send(Raws.IRCX_ERR_ERRONEOUSNICK_432(chatFrame.Server, chatFrame.User, nickname));
            return false;
        }

        chatFrame.User.Nickname = nickname;
        return true;
    }
}