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
    /*
       ACCESS <object> <LIST|ADD|DELETE|CLEAR> <level> <mask> [<timeout> [:<reason>]]
     
       Syntax 1: ACCESS <object> LIST
          
       Syntax 2: ACCESS <object> ADD|DELETE <level> <mask>
       [<timeout> [:<reason>]]
          
       Syntax 3: ACCESS <object> CLEAR [<level>]
     */
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

    private void ClearAccess(IChatFrame chatFrame, IChatObject targetObject)
    {
        var parameters = chatFrame.ChatMessage.Parameters.TakeLast(chatFrame.ChatMessage.Parameters.Count - 2).ToList();

        var accessLevel = EnumAccessLevel.All;
        if (parameters.Count > 0)
            if (!Enum.TryParse(parameters[0], true, out accessLevel))
            {
                // Bad level
                chatFrame.User.Send(Raws.IRCX_ERR_BADLEVEL_903(chatFrame.Server, chatFrame.User, targetObject));
                return;
            }

        if (accessLevel != EnumAccessLevel.All)
        {
            var canModify = targetObject.Access.CanModifyAccessLevel((IChatObject)chatFrame.User, targetObject, accessLevel);
            if (!canModify)
            {
                chatFrame.User.Send(Raws.IRCX_ERR_BADLEVEL_903(chatFrame.Server, chatFrame.User, targetObject));
                return;
            }
        }
        
        var accessResult = targetObject.Access.Clear(
            (IChatObject)chatFrame.User, targetObject, chatFrame.User.GetLevel(), accessLevel);
        
        if (accessResult == EnumAccessError.IRCERR_INCOMPLETE)
        {
            // Some entries were not cleared due to ...
            chatFrame.User.Send(Raws.IRCX_ERR_ACCESSNOTCLEAR_922(chatFrame.Server, chatFrame.User));
        }
        else if (accessResult == EnumAccessError.SUCCESS)
        {
            chatFrame.User.Send(Raws.IRCX_RPL_ACCESSCLEAR_820(chatFrame.Server, chatFrame.User, targetObject,
                accessLevel));
        }
        else
        {
            chatFrame.User.Send(Raws.IRCX_ERR_NOACCESS_913(chatFrame.Server, chatFrame.User, targetObject));
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
            chatFrame.User.Send(Raws.IRCX_ERR_BADLEVEL_903(chatFrame.Server, chatFrame.User, targetObject));
            return;
        }

        var mask = parameters[1];
        
        // Get access
        var accessEntry = targetObject.Access.Get(accessLevel, mask);
        if (accessEntry == null)
        {
            // No such access entry
            chatFrame.User.Send(Raws.IRCX_ERR_MISACCESS_915(chatFrame.Server, chatFrame.User));
            return;
        }
        
        var canModify = targetObject.Access.CanModifyAccessEntry((IChatObject)chatFrame.User, targetObject, accessEntry);
        if (!canModify)
        {
            chatFrame.User.Send(Raws.IRCX_ERR_NOACCESS_913(chatFrame.Server, chatFrame.User, targetObject));
            return;
        }
        
        var accessError = targetObject.Access.Delete(accessEntry);

        if (accessError == EnumAccessError.IRCERR_NOACCESS)
            chatFrame.User.Send(Raws.IRCX_ERR_DUPACCESS_914(chatFrame.Server, chatFrame.User));
        else if (accessError == EnumAccessError.SUCCESS)
            // RPL Access Add
            chatFrame.User.Send(Raws.IRCX_RPL_ACCESSDELETE_802(chatFrame.Server, chatFrame.User, targetObject,
                accessEntry.AccessLevel.ToString(), accessEntry.Mask, accessEntry.Timeout, accessEntry.EntryAddress, accessEntry.Reason));
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
            chatFrame.User.Send(Raws.IRCX_ERR_BADLEVEL_903(chatFrame.Server, chatFrame.User, targetObject));
            return;
        }

        if (!targetObject.Access.CanModifyAccessLevel((IChatObject)chatFrame.User, targetObject, accessLevel))
        {
            chatFrame.User.Send(Raws.IRCX_ERR_NOACCESS_913(chatFrame.Server, chatFrame.User, targetObject));
            return;
        }

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
        var accessError = targetObject.Access.Add(entry);

        if (accessError == EnumAccessError.IRCERR_DUPACCESS)
            chatFrame.User.Send(Raws.IRCX_ERR_DUPACCESS_914(chatFrame.Server, chatFrame.User));
        if (accessError == EnumAccessError.IRCERR_BADLEVEL)
        {
            chatFrame.User.Send(Raws.IRCX_ERR_BADLEVEL_903(chatFrame.Server, chatFrame.User, targetObject));
        }
        else if (accessError == EnumAccessError.SUCCESS)
            // RPL Access Add
            chatFrame.User.Send(Raws.IRCX_RPL_ACCESSADD_801(chatFrame.Server, chatFrame.User, targetObject,
                entry.AccessLevel.ToString(), entry.Mask, entry.Timeout, entry.EntryAddress, entry.Reason));
    }

    private void ListAccess(IChatFrame chatFrame, IChatObject targetObject)
    {
        // If not at least host then no access
        if (!targetObject.Access.CanModifyAccessLevel((IChatObject)chatFrame.User, targetObject, EnumAccessLevel.HOST))
        {
            chatFrame.User.Send(Raws.IRCX_ERR_NOACCESS_913(chatFrame.Server, chatFrame.User, targetObject));
            return;
        }
        
        chatFrame.User.Send(Raws.IRCX_RPL_ACCESSSTART_803(chatFrame.Server, chatFrame.User, targetObject));

        // TODO: Some entries were not listed due to level restriction
        // :TK2CHATCHATA01 804 'Admin_Koach * DENY *!96E5C937AE1CEFB3@*$* 2873 Sysop_Wondrously@cg :Violation of MSN Code of Conduct - 6-US
        targetObject.Access.GetEntries().Values.ToList().ForEach(
            list => list.ForEach(entry =>
                chatFrame.User.Send(Raws.IRCX_RPL_ACCESSLIST_804(chatFrame.Server, chatFrame.User, targetObject,
                    entry.AccessLevel.ToString(), entry.Mask, (int)Math.Ceiling(entry.Ttl.TotalMinutes),
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
