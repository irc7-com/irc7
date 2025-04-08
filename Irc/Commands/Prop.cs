using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;
using Irc.Objects.Channel;

public class Prop : Command, ICommand
{
    public Prop() : base(2, false)
    {
    }

    public new EnumCommandDataType GetDataType()
    {
        return EnumCommandDataType.None;
    }

    public new void Execute(IChatFrame chatFrame)
    {
        //chatFrame.User.Send(Raws.IRCX_ERR_NOTIMPLEMENTED(chatFrame.Server, chatFrame.User, nameof(Access)));
        // Passport hack
        //chatFrame.User.Name = "Sky";
        //chatFrame.User.GetAddress().Nickname = "Sky";
        //chatFrame.User.GetAddress().User = "A65F0CE7D05F0B4E";
        //chatFrame.User.GetAddress().Host = "GateKeeperPassport";
        //chatFrame.User.GetAddress().RealName = "Sky";
        //chatFrame.User.GetAddress().Server = "SERVER";
        //chatFrame.User.Register();
        // Register.Execute(chatFrame);

        // TODO: Resolve object first, e.g. IChatServer.GetObject(string)

        var objectName = chatFrame.ChatMessage.Parameters.First();
        if (!chatFrame.User.IsRegistered())
        {
            if (chatFrame.User.IsAuthenticated())
                // Prop $ NICK
                if (objectName == "$")
                    if (chatFrame.ChatMessage.Parameters.Count >= 2)
                    {
                        // TODO: This needs rewriting
                        if (string.Compare("NICK", chatFrame.ChatMessage.Parameters[1], true) == 0)
                        {
                            chatFrame.User.Nickname = chatFrame.User.Name;
                            SendProp(chatFrame.Server, chatFrame.User, (IExtendedChatObject)chatFrame.User, "NICK",
                                chatFrame.User.Name);
                        }
                        else if (string.Compare("MSNREGCOOKIE", chatFrame.ChatMessage.Parameters[1], true) == 0)
                        {
                            if (chatFrame.ChatMessage.Parameters.Count >= 3)
                            {
                                var regcookie = chatFrame.ChatMessage.Parameters[2];
                                chatFrame.Server.ProcessCookie(chatFrame.User, "MSNREGCOOKIE",
                                    regcookie);
                            }
                        }
                        else if (string.Compare("SUBSCRIBERINFO", chatFrame.ChatMessage.Parameters[1], true) == 0)
                        {
                            var subscriberinfo = chatFrame.ChatMessage.Parameters[2];
                            chatFrame.Server.ProcessCookie(chatFrame.User, "SUBSCRIBERINFO",
                                subscriberinfo);
                        }
                        else if (string.Compare("MSNPROFILE", chatFrame.ChatMessage.Parameters[1], true) == 0)
                        {
                            // TODO: Hook up to actual prop
                            var msnprofile = chatFrame.ChatMessage.Parameters[2];
                            ((IServer)chatFrame.Server).ProcessCookie(chatFrame.User, "MSNPROFILE",
                                msnprofile);
                        }
                        else if (string.Compare("ROLE", chatFrame.ChatMessage.Parameters[1], true) == 0)
                        {
                            var role = chatFrame.ChatMessage.Parameters[2];
                            ((IServer)chatFrame.Server).ProcessCookie(chatFrame.User, "ROLE", role);
                        }
                        else
                        {
                            chatFrame.User.Send(Raws.IRCX_ERR_BADPROPERTY_905(chatFrame.Server, chatFrame.User,
                                chatFrame.ChatMessage.Parameters[1]));
                        }
                    }
            // PROP $ MSNREGCOOKIE
            // If regcookie is prop'd then no user is required, this fills in the USER info
            // Performs a NICK command
            // You have not authenticated or registered or whatever
        }
        else
        {
            IExtendedChatObject? chatObject;

            // <$> The $ value is used to indicate the user that originated the request.
            if (objectName == "$")
                chatObject = (IExtendedChatObject?)chatFrame.User;
            else
                chatObject = (IExtendedChatObject?)chatFrame.Server.GetChatObject(objectName);

            if (chatObject == null)
            {
                // No such object
                chatFrame.User.Send(Raws.IRCX_ERR_NOSUCHOBJECT_924(chatFrame.Server, chatFrame.User, objectName));
            }
            else
            {
                var props = new List<IPropRule>();
                if (chatFrame.ChatMessage.Parameters.Count >= 3)
                {
                    var propValue = chatFrame.ChatMessage.Parameters[2];

                    // Setter
                    // TODO: Needs refactoring
                    var prop = chatObject.PropCollection.GetProp(chatFrame.ChatMessage.Parameters[1]);
                    if (prop != null)
                    {
                        if (chatObject.CanBeModifiedBy((ChatObject)chatFrame.User))
                        {
                            var ircError = prop.EvaluateSet((IChatObject)chatFrame.User, chatObject, propValue);
                            if (ircError == EnumIrcError.ERR_NOPERMS)
                            {
                                chatFrame.User.Send(Raws.IRCX_ERR_NOACCESS_913(chatFrame.Server, chatFrame.User,
                                    chatObject));
                                return;
                            }

                            if (ircError == EnumIrcError.ERR_BADVALUE)
                            {
                                chatFrame.User.Send(Raws.IRCX_ERR_BADVALUE_906(chatFrame.Server, chatFrame.User,
                                    propValue));
                                return;
                            }

                            if (ircError == EnumIrcError.OK)
                            {
                                prop.SetValue(propValue);
                                chatObject.Send(
                                    Raws.RPL_PROP_IRCX(chatFrame.Server, chatFrame.User, (ChatObject)chatObject,
                                        prop.Name, propValue), prop.WriteAccessLevel);
                            }
                        }
                        else
                        {
                            chatFrame.User.Send(Raws.IRCX_ERR_NOACCESS_913(chatFrame.Server, chatFrame.User,
                                chatObject));
                        }
                    }
                    else
                    {
                        // Bad prop
                        chatFrame.User.Send(Raws.IRCX_ERR_BADPROPERTY_905(chatFrame.Server, chatFrame.User,
                            objectName));
                    }
                }
                else
                {
                    // Getter

                    if (chatFrame.ChatMessage.Parameters[1] == "*")
                    {
                        props.AddRange(chatObject.PropCollection.GetProps());
                    }
                    else
                    {
                        var prop = chatObject.PropCollection.GetProp(chatFrame.ChatMessage.Parameters[1]);
                        if (prop != null)
                            props.Add(prop);
                        else
                            // Bad prop
                            chatFrame.User.Send(Raws.IRCX_ERR_BADPROPERTY_905(chatFrame.Server, chatFrame.User,
                                objectName));
                    }

                    if (props.Count > 0) SendProps(chatFrame.Server, chatFrame.User, chatObject, props);
                }
            }
        }
    }

    // TODO: Rewrite this code
    public void SendProps(IServer server, IUser user, IExtendedChatObject targetObject, List<IPropRule> props)
    {
        var propsSent = 0;
        foreach (var prop in props)
        {
            if (prop.EvaluateGet((IChatObject)user, targetObject) == EnumIrcError.ERR_NOPERMS)
            {
                if (props.Count == 1) user.Send(Raws.IRCX_ERR_SECURITY_908(server, user));
                continue;
            }

            if (targetObject is Channel)
            {
                var kvp = user.GetChannels().FirstOrDefault(x => x.Key == targetObject);
                if (kvp.Value != null)
                {
                    var member = kvp.Value;
                    var propValue = prop.GetValue(targetObject);
                    if (!string.IsNullOrEmpty(propValue))
                    {
                        SendProp(server, user, targetObject, prop.Name, propValue);
                        propsSent++;
                    }
                }
            }
            else
            {
                SendProp(server, user, targetObject, prop.Name, prop.GetValue(targetObject));
            }

            propsSent++;
        }

        if (propsSent > 0) user.Send(Raws.IRCX_RPL_PROPEND_819(server, user, targetObject));
    }

    public void SendProp(IServer server, IUser user, IExtendedChatObject targetObject, string propName,
        string propValue)
    {
        user.Send(Raws.IRCX_RPL_PROPLIST_818(server, user, targetObject, propName, propValue));
    }
}