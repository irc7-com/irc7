namespace Irc;

public static class IrcObjectIdentifiers
{
    public enum ObjIdentifier
    {
        ObjIdGlobalChannel = 0x1,
        ObjIdLocalChannel = 0x2,
        ObjIdExtendedGlobalChannel = 0x3,
        ObjIdExtendedLocalChannel = 0x4,
        ObjIdLastChannel = 0xF,
        ObjIdIRCUser = 0x5,
        ObjIdIRCUserUnicode = 0x6,
        ObjIdIRCUserHex = 0x7,
        ObjIdInternal = 0xE,
        ObjIdNetwork = 0xD,
        ObjIdServer = 0xC,
        InvalidObjId = 0xFF
    }

    public static ObjIdentifier IdentifyObject(string ObjectName)
    {
        if (ObjectName.Length == 1)
            switch (ObjectName[0])
            {
                case '$':
                {
                    return ObjIdentifier.ObjIdServer;
                }
                case '*':
                {
                    return ObjIdentifier.ObjIdNetwork;
                }
                case '%':
                {
                    return ObjIdentifier.ObjIdLastChannel;
                }
            }
        else if (ObjectName.Length > 1)
            switch (ObjectName[0])
            {
                case '%':
                {
                    switch (ObjectName[1])
                    {
                        case '#':
                        {
                            return ObjIdentifier.ObjIdExtendedGlobalChannel;
                        }
                        case '&':
                        {
                            return ObjIdentifier.ObjIdExtendedLocalChannel;
                        }
                        default:
                        {
                            return ObjIdentifier.InvalidObjId;
                        }
                    }
                }
                case '^':
                {
                    return ObjIdentifier.ObjIdIRCUserHex;
                }
                case '0':
                {
                    return ObjIdentifier.ObjIdInternal;
                }
                case '\'':
                {
                    return ObjIdentifier.ObjIdIRCUserUnicode;
                }
                case '#':
                {
                    return ObjIdentifier.ObjIdGlobalChannel;
                }
                case '&':
                {
                    return ObjIdentifier.ObjIdLocalChannel;
                }
            }

        // Default is user nickname
        return ObjIdentifier.ObjIdIRCUser;
    }

    public static bool IsObject(string Name)
    {
        // Rule that an object must begin with 0
        if (Name.Length > 0)
            if (Name[0] == 48)
                return true;

        return false;
    }
}