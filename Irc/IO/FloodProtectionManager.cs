using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;
using Irc.Objects.User;

namespace Irc.IO;
/*
 * If the service receives one of these messages, it temporarily suspends the user's session. After the delay expires, the service resumes normal message processing.
 * Also, if a user tries to join a channel using the wrong password, the service delays processing subsequent attempts by that user to join the channel. 
 */

/*
    [08:52] <.>Sky> The way the flood protection settings works is that it delays the message queue of that user only, so if you are for example pasting 100 lines in a
 *                  channel and subsequently try and do anything else in the queue it wont process that until the rest of the buffer is processed
    [08:52] <.>Sky> if for example you do a very expensive operation and paste 100 lines in a channel with a 2 second delay for message processing
    [08:52] <.>Sky> you have to wait 100 * 2 (200 seconds) to be able to do anything else, be it a version or whatever
    [08:53] <.>Sky> this is what Exchange has demonstrated to me so far and this is how I will implement it
 */

public class FloodProtectionManager : IFloodProtectionManager
{
    public EnumFloodResult FloodCheck(EnumCommandDataType type, User user)
    {
        return Audit(user.GetFloodProtectionProfile(), type, user.GetLevel());
    }

    public EnumFloodResult Audit(IFloodProtectionProfile protectionProfile, EnumCommandDataType type,
        EnumUserAccessLevel level)
    {
        if (level >= EnumUserAccessLevel.Guide) return EnumFloodResult.Ok;

        switch (type)
        {
            case EnumCommandDataType.None:
            {
                if (protectionProfile.GetFloodProtectionLevel().CanProcess)
                    return EnumFloodResult.Ok;
                return EnumFloodResult.Wait;
            }
            case EnumCommandDataType.Data:
            {
                if (protectionProfile.GetFloodProtectionLevel().CanProcessData)
                    return EnumFloodResult.Ok;
                return EnumFloodResult.Wait;
            }
            case EnumCommandDataType.Invitation:
            {
                if (protectionProfile.GetFloodProtectionLevel().CanProcessInvite)
                    return EnumFloodResult.Ok;
                return EnumFloodResult.Wait;
            }
            case EnumCommandDataType.Join:
            {
                if (protectionProfile.GetFloodProtectionLevel().CanProcessJoin)
                    return EnumFloodResult.Ok;
                return EnumFloodResult.Wait;
            }
            case EnumCommandDataType.WrongChannelPassword:
            {
                if (protectionProfile.GetFloodProtectionLevel().CanProcessPassword)
                    return EnumFloodResult.Ok;
                return EnumFloodResult.Wait;
            }
            case EnumCommandDataType.Standard:
            {
                if (protectionProfile.GetFloodProtectionLevel().CanProcessStandard)
                    return EnumFloodResult.Ok;
                return EnumFloodResult.Wait;
            }
            case EnumCommandDataType.HostMessage:
            {
                if (protectionProfile.GetFloodProtectionLevel().CanProcessHostMessage)
                    return EnumFloodResult.Ok;
                return EnumFloodResult.Wait;
            }
            default:
            {
                return EnumFloodResult.Ok;
            }
        }
    }
}