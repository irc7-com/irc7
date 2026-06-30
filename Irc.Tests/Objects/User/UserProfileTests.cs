using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.User;
using Moq;

namespace Irc.Tests.Objects.User;

[TestFixture]
public class UserProfileTests
{
    [Test]
    public void GetGenderString_NoProfileCode_ReturnsR()
    {
        var profile = new UserProfile { HasProfile = false };

        Assert.That(profile.GetGenderString(), Is.EqualTo("R"));
    }

    [Test]
    public void GetGenderString_HasProfile_NoGender_ReturnsP()
    {
        var profile = new UserProfile { HasProfile = true };

        Assert.That(profile.GetGenderString(), Is.EqualTo("P"));
    }

    [Test]
    public void GetGenderString_Male_ReturnsM()
    {
        var profile = new UserProfile { HasProfile = true, IsMale = true };

        Assert.That(profile.GetGenderString(), Is.EqualTo("M"));
    }

    [Test]
    public void GetGenderString_Female_ReturnsF()
    {
        var profile = new UserProfile { HasProfile = true, IsFemale = true };

        Assert.That(profile.GetGenderString(), Is.EqualTo("F"));
    }

    [Test]
    public void GetPictureString_HasPicture_ReturnsY()
    {
        var profile = new UserProfile { HasPicture = true };

        Assert.That(profile.GetPictureString(), Is.EqualTo("Y"));
    }

    [Test]
    public void GetPictureString_NoPicture_ReturnsX()
    {
        var profile = new UserProfile { HasPicture = false };

        Assert.That(profile.GetPictureString(), Is.EqualTo("X"));
    }

    [Test]
    public void GetProfileString_Male_HasPicture_ReturnsMY()
    {
        var profile = new UserProfile { HasProfile = true, IsMale = true, HasPicture = true };

        Assert.That(profile.GetProfileString(), Is.EqualTo("MY"));
    }

    [Test]
    public void GetProfileString_Female_HasPicture_ReturnsFY()
    {
        var profile = new UserProfile { HasProfile = true, IsFemale = true, HasPicture = true };

        Assert.That(profile.GetProfileString(), Is.EqualTo("FY"));
    }

    [Test]
    public void GetProfileString_HasProfile_NoPicture_ReturnsPX()
    {
        var profile = new UserProfile { HasProfile = true, HasPicture = false };

        Assert.That(profile.GetProfileString(), Is.EqualTo("PX"));
    }

    [Test]
    public void GetProfileString_NoProfileCode_ReturnsRX()
    {
        var profile = new UserProfile { HasProfile = false, HasPicture = false };

        Assert.That(profile.GetProfileString(), Is.EqualTo("RX"));
    }

    [Test]
    public void GetRegisteredString_Registered_ReturnsB()
    {
        var profile = new UserProfile { Registered = true };

        Assert.That(profile.GetRegisteredString(), Is.EqualTo("B"));
    }

    [Test]
    public void GetRegisteredString_NotRegistered_ReturnsO()
    {
        var profile = new UserProfile { Registered = false };

        Assert.That(profile.GetRegisteredString(), Is.EqualTo("O"));
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

[TestFixture]
public class UserFormattedProfileTests
{
    [Test]
    public void NoProfile_IsGuest_AndIrc8FlagIsGO()
    {
        var user = CreateUser();

        Assert.That(user.IsGuest(), Is.True,
            "A user with no Passport profile is a guest");
        Assert.That(user.GetFormattedProfile(EnumProtocolType.IRC8), Is.EqualTo("H,U,GO"),
            "No profile renders gender 'G' so clients hide View Profile");
    }

    [Test]
    public void PassportProfile_NotGuest_AndIrc8FlagIsRXO()
    {
        var user = CreateUser();
        user.AssignPassportProfile();

        Assert.That(user.IsGuest(), Is.False,
            "A user with a Passport profile is not a guest");
        Assert.That(user.GetFormattedProfile(EnumProtocolType.IRC8), Is.EqualTo("H,U,RXO"),
            "A passport user with no profile code reports 'RX', distinct from the no-profile 'G'");
    }

    [Test]
    public void PassportProfile_MaleWithPicture_Irc8_ReportsMYO()
    {
        var user = CreateUser();
        user.AssignPassportProfile();
        user.GetProfile()!.SetProfileCode(11);

        Assert.That(user.GetFormattedProfile(EnumProtocolType.IRC8), Is.EqualTo("H,U,MYO"));
    }

    [Test]
    public void NoProfile_Irc5_GenderOnly()
    {
        var user = CreateUser();

        Assert.That(user.GetFormattedProfile(EnumProtocolType.IRC5), Is.EqualTo("H,U,G"));
    }

    [Test]
    public void NoProfile_Irc7_GenderAndEmptyPicture()
    {
        var user = CreateUser();

        Assert.That(user.GetFormattedProfile(EnumProtocolType.IRC7), Is.EqualTo("H,U,G"));
    }

    [Test]
    public void PassportProfile_MaleWithPicture_Irc7_ReportsMY()
    {
        var user = CreateUser();
        user.AssignPassportProfile();
        user.GetProfile()!.SetProfileCode(11);

        Assert.That(user.GetFormattedProfile(EnumProtocolType.IRC7), Is.EqualTo("H,U,MY"));
    }

    [Test]
    public void AwayUser_NoProfile_Irc8_AwayCharIsG()
    {
        var user = CreateUser();
        user.Away = true;

        Assert.That(user.GetFormattedProfile(EnumProtocolType.IRC8), Is.EqualTo("G,U,GO"));
    }

    private static Irc.Objects.User.User CreateUser()
    {
        var mockConnection = new Mock<IConnection>();
        var mockProtocol = new Mock<IProtocol>();
        var mockDataRegulator = new Mock<IDataRegulator>();
        var mockFloodProtectionProfile = new Mock<IFloodProtectionProfile>();
        var mockServer = new Mock<IServer>();

        mockConnection.Setup(x => x.GetIp()).Returns("127.0.0.1");
        mockServer.Setup(s => s.DisableGuestMode).Returns(false);

        return new Irc.Objects.User.User(
            mockConnection.Object,
            mockProtocol.Object,
            mockDataRegulator.Object,
            mockFloodProtectionProfile.Object,
            mockServer.Object,
            _ => new Mock<ISaslHandler>().Object);
    }
}
