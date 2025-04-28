using Irc.Constants;
using Irc.Enumerations;
using Irc.Helpers;
using Irc.Interfaces;

namespace Irc.Commands;

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
        if (!chatFrame.User.IsAuthenticated()) HandlePreauthNicknameChange(chatFrame);
        else if (!chatFrame.User.IsRegistered()) HandlePreregNicknameChange(chatFrame);
        else HandleRegNicknameChange(chatFrame);
    }

    public static bool ValidateNickname(string nickname, bool guest = false, bool oper = false, bool preAuth = false,
        bool preReg = false, bool isDs = false)
    {
        var mask = Resources.PostAuthNicknameMask;

        if (preAuth) mask = Resources.PreAuthNicknameMask;
        else if (oper) mask = Resources.PostAuthOperNicknameMask;
        else if (guest) mask = Resources.PostAuthGuestNicknameMask;

        if (isDs) mask = Resources.DsNickname;

        var isInLength = nickname.Length <= Resources.MaxFieldLen;
        var isMatch = RegularExpressions.Match(mask, nickname, true);
        var isValid = isInLength && isMatch;
        return isValid;
    }

    public static bool HandlePreauthNicknameChange(IChatFrame chatFrame)
    {
        var nickname = chatFrame.ChatMessage.Parameters.First();
        // UTF8 / Guest / Normal / Admin/Sysop/Guide OK
        var isValid = ValidateNickname(nickname, preAuth: true, isDs: chatFrame.Server.IsDirectoryServer); 
        if (!isValid)
        {
            chatFrame.User.Send(Raws.IRCX_ERR_ERRONEOUSNICK_432(chatFrame.Server, chatFrame.User, nickname));
            return false;
        }

        chatFrame.User.Nickname = nickname;
        return true;
    }

    public static bool HandlePreregNicknameChange(IChatFrame chatFrame)
    {
        var nickname = chatFrame.ChatMessage.Parameters.First();
        var guest = chatFrame.User.IsGuest();
        var oper = chatFrame.User.GetLevel() >= EnumUserAccessLevel.Guide;

        if (!ValidateNickname(nickname, guest, oper, false, true, isDs: chatFrame.Server.IsDirectoryServer))
        {
            chatFrame.User.Send(Raws.IRCX_ERR_ERRONEOUSNICK_432(chatFrame.Server, chatFrame.User, nickname));
            return false;
        }

        chatFrame.User.Nickname = nickname;
        return true;
    }

    public static bool HandleRegNicknameChange(IChatFrame chatFrame)
    {
        var nickname = chatFrame.ChatMessage.Parameters.First();
        var guest = chatFrame.User.IsGuest();
        var oper = chatFrame.User.GetLevel() >= EnumUserAccessLevel.Guide;

        if (!guest && !oper)
        {
            chatFrame.User.Send(Raws.IRCX_ERR_NONICKCHANGES_439(chatFrame.Server, chatFrame.User, nickname));
            return false;
        }

        var channels = chatFrame.User.GetChannels();
        foreach (var channel in channels)
        foreach (var member in channel.Key.GetMembers())
            if (member.GetUser().Nickname == nickname)
            {
                chatFrame.User.Send(Raws.IRCX_ERR_NICKINUSE_433(chatFrame.Server, chatFrame.User));
                return false;
            }

        if (!ValidateNickname(nickname, guest, oper, isDs: chatFrame.Server.IsDirectoryServer))
        {
            chatFrame.User.Send(Raws.IRCX_ERR_ERRONEOUSNICK_432(chatFrame.Server, chatFrame.User, nickname));
            return false;
        }

        chatFrame.User.ChangeNickname(nickname, false);
        return true;
    }
}