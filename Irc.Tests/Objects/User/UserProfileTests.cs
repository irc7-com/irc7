using Irc.Enumerations;
using Irc.Objects.User;

namespace Irc.Tests.Objects.User;

[TestFixture]
public class UserProfileTests
{
    [Test]
    public void GetGenderString_Guest_ReturnsG()
    {
        var profile = new UserProfile { Guest = true };

        Assert.That(profile.GetGenderString(), Is.EqualTo("G"));
    }

    [Test]
    public void GetPictureString_Guest_ReturnsEmpty()
    {
        var profile = new UserProfile { Guest = true };

        Assert.That(profile.GetPictureString(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetGenderString_NonGuest_NoPuid_ReturnsG()
    {
        var profile = new UserProfile { Guest = false, HasPuid = false };

        Assert.That(profile.GetGenderString(), Is.EqualTo("G"));
    }

    [Test]
    public void GetPictureString_NonGuest_NoPuid_ReturnsEmpty()
    {
        var profile = new UserProfile { Guest = false, HasPuid = false };

        Assert.That(profile.GetPictureString(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void ToString_NonGuest_NoPuid_NotRegistered_EndsWithGO()
    {
        var profile = new UserProfile
        {
            Guest = false,
            HasPuid = false,
            Registered = false,
            Away = false,
            Level = EnumUserAccessLevel.None
        };

        var result = profile.ToString();

        Assert.That(result, Does.EndWith("GO"),
            $"Expected profile string to end with 'GO' for a no-PUID non-guest user; got '{result}'");
    }

    [Test]
    public void GetGenderString_HasPuid_NoProfile_ReturnsR()
    {
        var profile = new UserProfile { Guest = false, HasPuid = true, HasProfile = false };

        Assert.That(profile.GetGenderString(), Is.EqualTo("R"));
    }

    [Test]
    public void GetProfileString_HasPuid_Male_HasPicture_ReturnsMY()
    {
        var profile = new UserProfile
        {
            Guest = false,
            HasPuid = true,
            HasProfile = true,
            IsMale = true,
            HasPicture = true
        };

        Assert.That(profile.GetProfileString(), Is.EqualTo("MY"));
    }

    [Test]
    public void GetProfileString_HasPuid_Female_HasPicture_ReturnsFY()
    {
        var profile = new UserProfile
        {
            Guest = false,
            HasPuid = true,
            HasProfile = true,
            IsFemale = true,
            HasPicture = true
        };

        Assert.That(profile.GetProfileString(), Is.EqualTo("FY"));
    }

    [Test]
    public void GetProfileString_HasPuid_HasProfile_NoPicture_ReturnsPX()
    {
        var profile = new UserProfile
        {
            Guest = false,
            HasPuid = true,
            HasProfile = true,
            IsMale = false,
            IsFemale = false,
            HasPicture = false
        };

        Assert.That(profile.GetProfileString(), Is.EqualTo("PX"));
    }

    [Test]
    public void GetGenderString_Guest_OverridesHasPuid_ReturnsG()
    {
        var profile = new UserProfile
        {
            Guest = true,
            HasPuid = true,
            HasProfile = true,
            IsMale = true
        };

        Assert.That(profile.GetGenderString(), Is.EqualTo("G"));
    }

    [Test]
    public void GetPictureString_Guest_OverridesHasPuid_ReturnsEmpty()
    {
        var profile = new UserProfile
        {
            Guest = true,
            HasPuid = true,
            HasPicture = true
        };

        Assert.That(profile.GetPictureString(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetGenderString_NoPuid_HasProfile_Male_ReturnsG()
    {
        var profile = new UserProfile
        {
            Guest = false,
            HasPuid = false,
            HasProfile = true,
            IsMale = true
        };

        Assert.That(profile.GetGenderString(), Is.EqualTo("G"),
            "HasProfile/IsMale must be irrelevant when HasPuid is false");
    }

    [Test]
    public void GetProfileString_DeterministicForSameState()
    {
        var profile = new UserProfile
        {
            Guest = false,
            HasPuid = true,
            HasProfile = true,
            IsMale = true,
            HasPicture = false
        };

        var first = profile.GetProfileString();
        var second = profile.GetProfileString();

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void SetProfileCode_RoundTrip_ReturnsOriginalCode()
    {
        for (var code = 0; code <= 15; code++)
        {
            var profile = new UserProfile();
            profile.SetProfileCode(code);

            Assert.That(profile.GetProfileCode(), Is.EqualTo(code),
                $"Profile code {code} did not round-trip");
        }
    }
}
