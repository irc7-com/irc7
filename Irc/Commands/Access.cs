using Irc.Access;
using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Irc.Objects.Server;
using Irc.Objects.User;

internal class Access : Command, ICommand
{
    public Access() : base(1)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        var objectName = chatFrame.ChatMessage.Parameters.First();
        var accessCommandName = AccessCommand.LIST.ToString();
        if (chatFrame.ChatMessage.Parameters.Count > 1) accessCommandName = chatFrame.ChatMessage.Parameters[1];

        if (!Enum.TryParse(accessCommandName, true, out AccessCommand accessCommand))
        {
            // Bad Command
            chatFrame.User.Send(Raws.IRCX_ERR_BADCOMMAND_900(chatFrame.Server, chatFrame.User, accessCommandName));
            return;
        }

        var targetObject = (IChatObject?)chatFrame.Server.GetChatObject(objectName);
        if (targetObject == null)
        {
            // No such object
            chatFrame.User.Send(Raws.IRCX_ERR_NOSUCHOBJECT_924(chatFrame.Server, chatFrame.User, objectName));
            return;
        }

        if (!CanModify(chatFrame, targetObject))
        {
            chatFrame.User.Send(Raws.IRCX_ERR_SECURITY_908(chatFrame.Server, chatFrame.User));
            // No permissions
            return;
        }

        switch (accessCommand)
        {
            case AccessCommand.LIST:
            {
                ListAccess(chatFrame, targetObject);
                break;
            }
            case AccessCommand.ADD:
            {
                AddAccess(chatFrame, targetObject);
                break;
            }
            case AccessCommand.DELETE:
            {
                DeleteAccess(chatFrame, targetObject);
                break;
            }
            case AccessCommand.CLEAR:
            {
                ClearAccess(chatFrame, targetObject);
                break;
            }
        }
    }

    // TODO: The below should be offloaded to the respective Access class
    private bool CanModify(IChatFrame chatFrame, IChatObject targetObject)
    {
        if (targetObject is Server && !chatFrame.User.IsAdministrator())
            // No Access
            return false;

        if (targetObject is User && targetObject != chatFrame.User)
            // No Access
            return false;

        if (targetObject is Channel)
        {
            var channel = (IChannel)targetObject;
            var member = channel.GetMember(chatFrame.User);

            if (member == null || (!member.IsHost() && !member.IsOwner())) return false;
        }

        return true;
    }

    private void ClearAccess(IChatFrame chatFrame, IChatObject targetObject)
    {
        var parameters = chatFrame.ChatMessage.Parameters.TakeLast(chatFrame.ChatMessage.Parameters.Count - 2).ToList();

        var accessLevel = EnumAccessLevel.All;
        if (parameters.Count > 0)
            if (!Enum.TryParse(parameters[0], true, out accessLevel))
            {
                // Bad level
                chatFrame.User.Send(Raws.IRCX_ERR_BADLEVEL_903(chatFrame.Server, chatFrame.User, parameters[0]));
                return;
            }

        var accessResult = targetObject.AccessList.Clear(chatFrame.User.GetLevel(), accessLevel);
        if (accessResult == EnumAccessError.IRCERR_INCOMPLETE)
        {
            // Some entries were not cleared due to ...
        }
        else
        {
            chatFrame.User.Send(Raws.IRCX_RPL_ACCESSCLEAR_820(chatFrame.Server, chatFrame.User, targetObject,
                accessLevel));
        }
    }

    private void DeleteAccess(IChatFrame chatFrame, IChatObject targetObject)
    {
        // ACCESS <object> ADD|DELETE <level> <mask>

        var parameters = chatFrame.ChatMessage.Parameters.TakeLast(chatFrame.ChatMessage.Parameters.Count - 2).ToList();

        if (parameters.Count < 2)
            // Not enough parameters
            return;

        if (!Enum.TryParse<EnumAccessLevel>(parameters[0], true, out var accessLevel))
        {
            // Bad level
            chatFrame.User.Send(Raws.IRCX_ERR_BADLEVEL_903(chatFrame.Server, chatFrame.User, parameters[0]));
            return;
        }

        // TODO: Channel Access Level check

        var mask = parameters[1];
        var entry = new AccessEntry(mask, chatFrame.User.GetLevel(), accessLevel, mask, 0, string.Empty);
        var accessError = targetObject.AccessList.Delete(entry);

        if (accessError == EnumAccessError.IRCERR_NOACCESS)
            chatFrame.User.Send(Raws.IRCX_ERR_DUPACCESS_914(chatFrame.Server, chatFrame.User));
        else if (accessError == EnumAccessError.SUCCESS)
            // RPL Access Add
            chatFrame.User.Send(Raws.IRCX_RPL_ACCESSDELETE_802(chatFrame.Server, chatFrame.User, targetObject,
                entry.EntryLevel.ToString(), entry.Mask, entry.Timeout, entry.EntryAddress, entry.Reason));
    }

    private void AddAccess(IChatFrame chatFrame, IChatObject targetObject)
    {
        // ACCESS <object> ADD|DELETE <level> <mask> [< timeout > [:< reason >]]

        var parameters = chatFrame.ChatMessage.Parameters.TakeLast(chatFrame.ChatMessage.Parameters.Count - 2).ToList();

        if (parameters.Count < 2)
            // Not enough parameters
            return;

        if (!Enum.TryParse<EnumAccessLevel>(parameters[0], true, out var accessLevel))
        {
            // Bad level
            chatFrame.User.Send(Raws.IRCX_ERR_BADLEVEL_903(chatFrame.Server, chatFrame.User, parameters[0]));
            return;
        }

        // TODO: Channel Access Level check

        var mask = parameters[1];
        var timeout = 0;
        var reason = string.Empty;

        if (parameters.Count > 2)
            if (!int.TryParse(parameters[2], out timeout) || timeout < 0 || timeout > 999999)
                chatFrame.User.Send(Raws.IRCX_ERR_BADCOMMAND_900(chatFrame.Server, chatFrame.User, parameters[0]));
        // Bad command
        if (parameters.Count > 3) reason = parameters[3];

        // TODO: Solve below level issue
        var entry = new AccessEntry(chatFrame.User.GetAddress().GetUserHost(), chatFrame.User.GetLevel(), accessLevel,
            mask, timeout, reason);
        var accessError = targetObject.AccessList.Add(entry);

        if (accessError == EnumAccessError.IRCERR_DUPACCESS)
            chatFrame.User.Send(Raws.IRCX_ERR_DUPACCESS_914(chatFrame.Server, chatFrame.User));
        else if (accessError == EnumAccessError.SUCCESS)
            // RPL Access Add
            chatFrame.User.Send(Raws.IRCX_RPL_ACCESSADD_801(chatFrame.Server, chatFrame.User, targetObject,
                entry.AccessLevel.ToString(), entry.Mask, entry.Timeout, entry.EntryAddress, entry.Reason));
    }

    private void ListAccess(IChatFrame chatFrame, IChatObject targetObject)
    {
        chatFrame.User.Send(Raws.IRCX_RPL_ACCESSSTART_803(chatFrame.Server, chatFrame.User, targetObject));

        // TODO: Some entries were not listed due to level restriction
        // :TK2CHATCHATA01 804 'Admin_Koach * DENY *!96E5C937AE1CEFB3@*$* 2873 Sysop_Wondrously@cg :Violation of MSN Code of Conduct - 6-US
        targetObject.AccessList.GetEntries().Values.ToList().ForEach(
            list => list.ForEach(entry =>
                chatFrame.User.Send(Raws.IRCX_RPL_ACCESSLIST_804(chatFrame.Server, chatFrame.User, targetObject,
                    entry.AccessLevel.ToString(), entry.Mask, entry.Ttl.Minutes,
                    entry.EntryAddress, entry.Reason))
            )
        );

        chatFrame.User.Send(Raws.IRCX_RPL_ACCESSEND_805(chatFrame.Server, chatFrame.User, targetObject));
    }

    private enum AccessCommand
    {
        LIST,
        ADD,
        DELETE,
        CLEAR
    }
}