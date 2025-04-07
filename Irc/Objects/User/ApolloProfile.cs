using Irc.Enumerations;

namespace Irc.Objects.User;
/*
// TODO: MSNPROFILE
(Code) - (MSNPROFILE) - (Description)
FY - 13 - Female, has picture in profile.
MY - 11 - Male, has picture in profile.
PY - 9 - Gender not specified, has picture in profile.
FX - 5 - Female, no picture in profile.
MX - 3 - Male, no picture in profile.
PX - 1 - Gender not specified, and no picture, but has a profile.
RX - 0 - No profile at all.
G - 0 - Guest user (Guests can't have profiles).*/

/*
 *
 * <profile><male><female><picture>
FY 1011
MY 1101
PY 1001
FX 1010
MX 1100
PX 1000
RX 0000
*/
/*where:
<H|G> is the away status using the same notation as the WHO message  (H=here, G=gone) (IRC4+)
<A|S|G|U> is the user mode (Admin, Sysop, Guide, User) (IRC4+)
<G|R|P|M|F> is the user type (G = Guest IRC4+, R = Registered or Passport user IRC4+, P = No Gender IRC7, M = Male IRC7, F = Female IRC7)
<X|Y> (X = No Pic, Y = Has Pic) (IRC7)
<B|O> (B = Registered, O = Unregistered) (IRC8)
[.|@|+] : This is optional depending on the user. Owners get a [.] prefix, Hosts
has a [@] prefix, and [+] prefix means the user has voice.
<nickname> = Nickname of up to 24 characters.


Admin,Sysop,Guide status was Guest as no profile was present. Therefore:
IRC8 H,A,GO is correct as well as
     H,U,GO etc

*/

public class ApolloProfile
{
    public EnumUserAccessLevel Level { get; set; }
    public bool Away { get; set; }
    public bool Guest { get; set; }
    public bool Registered { get; set; }
    public bool HasProfile { get; set; }
    public bool IsMale { get; set; }
    public bool IsFemale { get; set; }
    public bool HasPicture { get; set; }
    public bool IsSubscriber { get; set; }

    public int GetProfileCode()
    {
        return Convert.ToInt32(HasProfile) +
               (Convert.ToInt32(IsMale) << 1) +
               (Convert.ToInt32(IsFemale) << 2) +
               (Convert.ToInt32(HasPicture) << 3);
    }

    public void SetProfileCode(int code)
    {
        HasProfile = Convert.ToBoolean(code & 1);
        IsMale = Convert.ToBoolean(code & 2);
        IsFemale = Convert.ToBoolean(code & 4);
        HasPicture = Convert.ToBoolean(code & 8);
    }

    public string GetAwayString()
    {
        return Away ? "G" : "H";
    }

    public string GetRegisteredString()
    {
        return Registered ? "B" : "O";
    }

    public string GetPictureString()
    {
        return Guest ? "" : HasPicture ? "Y" : "X";
    }

    public string GetProfileString()
    {
        return $"{GetGenderString()}{GetPictureString()}";
    }

    public string GetModeString()
    {
        switch (Level)
        {
            case EnumUserAccessLevel.Administrator:
            {
                return "A";
            }
            case EnumUserAccessLevel.Sysop:
            {
                return "S";
            }
            case EnumUserAccessLevel.Guide:
            {
                return "G";
            }
            default:
            {
                return "U";
            }
        }
    }

    public string GetGenderString()
    {
        if (Guest) return "G";
        if (!HasProfile) return "R";

        return !IsMale && !IsFemale ? "P" : IsMale ? "M" : "F";
    }

    public string Irc5_ToString()
    {
        return $"{GetAwayString()},{GetModeString()},{GetGenderString()}";
    }

    public string Irc7_ToString()
    {
        return $"{GetAwayString()},{GetModeString()},{GetProfileString()}";
    }

    public override string ToString()
    {
        return $"{GetAwayString()},{GetModeString()},{GetProfileString()}{GetRegisteredString()}";
    }
}