using Irc.Enumerations;
using Irc.Objects.User;
using NUnit.Framework;

namespace SSPI.GateKeeper.Tests;

public class UserProfileTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ApolloProfileTests_GetProfileStringTests()
    {
        var fy = new UserProfile
        {
            HasProfile = true,
            HasPicture = true,
            IsMale = false,
            IsFemale = true
        };

        Assert.That(13, Is.EqualTo(fy.GetProfileCode()));
        Assert.That("FY", Is.EqualTo(fy.GetProfileString()));

        var my = new UserProfile
        {
            HasProfile = true,
            HasPicture = true,
            IsMale = true,
            IsFemale = false
        };

        Assert.That(11, Is.EqualTo(my.GetProfileCode()));
        Assert.That("MY", Is.EqualTo(my.GetProfileString()));

        var py = new UserProfile
        {
            HasProfile = true,
            HasPicture = true,
            IsMale = false,
            IsFemale = false
        };

        Assert.That(9, Is.EqualTo(py.GetProfileCode()));
        Assert.That("PY", Is.EqualTo(py.GetProfileString()));

        var fx = new UserProfile
        {
            HasProfile = true,
            HasPicture = false,
            IsMale = false,
            IsFemale = true
        };

        Assert.That(5, Is.EqualTo(fx.GetProfileCode()));
        Assert.That("FX", Is.EqualTo(fx.GetProfileString()));

        var mx = new UserProfile
        {
            HasProfile = true,
            HasPicture = false,
            IsMale = true,
            IsFemale = false
        };

        Assert.That(3, Is.EqualTo(mx.GetProfileCode()));
        Assert.That("MX", Is.EqualTo(mx.GetProfileString()));

        var px = new UserProfile
        {
            HasProfile = true,
            HasPicture = false,
            IsMale = false,
            IsFemale = false
        };

        Assert.That(1, Is.EqualTo(px.GetProfileCode()));
        Assert.That("PX", Is.EqualTo(px.GetProfileString()));

        var rx = new UserProfile
        {
            HasProfile = false,
            HasPicture = false,
            IsMale = false,
            IsFemale = false
        };

        Assert.That(0, Is.EqualTo(rx.GetProfileCode()));
        Assert.That("RX", Is.EqualTo(rx.GetProfileString()));
    }

    [Test]
    public void ApolloProfileTests_GetModeStringTests()
    {
        var admin = new UserProfile
        {
            Level = EnumUserAccessLevel.Administrator
        };

        Assert.That("A", Is.EqualTo(admin.GetModeString()));

        var sysop = new UserProfile
        {
            Level = EnumUserAccessLevel.Sysop
        };

        Assert.That("S", Is.EqualTo(sysop.GetModeString()));

        var user = new UserProfile
        {
            Level = EnumUserAccessLevel.Member
        };

        Assert.That("U", Is.EqualTo(user.GetModeString()));
    }

    [Test]
    public void ApolloProfileTests_GetAwayStringTests()
    {
        var gone = new UserProfile
        {
            Away = true
        };
        Assert.That("G", Is.EqualTo(gone.GetAwayString()));

        var here = new UserProfile
        {
            Away = false
        };
        Assert.That("H", Is.EqualTo(here.GetAwayString()));
    }

    [Test]
    public void ApolloProfileTests_ToString()
    {
        var here_admin_guest = new UserProfile
        {
            Away = false,
            Level = EnumUserAccessLevel.Administrator,
            Guest = true
        };
        Assert.That("H,A,GO", Is.EqualTo(here_admin_guest.ToString()));

        var here_user_guest = new UserProfile
        {
            Away = false,
            Level = EnumUserAccessLevel.Member,
            Guest = true
        };
        Assert.That("H,U,GO", Is.EqualTo(here_user_guest.ToString()));

        var away_user_male_prof_registered = new UserProfile
        {
            Away = true,
            Level = EnumUserAccessLevel.Member,
            Guest = false,
            HasProfile = true,
            IsMale = true,
            Registered = true
        };
        Assert.That("G,U,MXB", Is.EqualTo(away_user_male_prof_registered.ToString()));

        var away_user_female_prof_pic_registered = new UserProfile
        {
            Away = true,
            Level = EnumUserAccessLevel.Member,
            Guest = false,
            HasProfile = true,
            IsMale = false,
            IsFemale = true,
            HasPicture = true,
            Registered = true
        };
        Assert.That("G,U,FYB", Is.EqualTo(away_user_female_prof_pic_registered.ToString()));
    }

    [Test]
    public void ApolloProfileTests_Irc5_ToString()
    {
        var here_admin_guest = new UserProfile
        {
            Away = false,
            Level = EnumUserAccessLevel.Administrator,
            Guest = true
        };
        Assert.That("H,A,G", Is.EqualTo(here_admin_guest.Irc5_ToString()));

        var here_user_guest = new UserProfile
        {
            Away = false,
            Level = EnumUserAccessLevel.Member,
            Guest = true
        };
        Assert.That("H,U,G", Is.EqualTo(here_user_guest.Irc5_ToString()));

        var away_user_male_prof_registered = new UserProfile
        {
            Away = true,
            Level = EnumUserAccessLevel.Member,
            Guest = false,
            HasProfile = true,
            IsMale = true,
            Registered = true
        };
        Assert.That("G,U,M", Is.EqualTo(away_user_male_prof_registered.Irc5_ToString()));
    }

    [Test]
    public void ApolloProfileTests_Irc7_ToString()
    {
        var here_admin_guest = new UserProfile
        {
            Away = false,
            Level = EnumUserAccessLevel.Administrator,
            Guest = true
        };
        Assert.That("H,A,G", Is.EqualTo(here_admin_guest.Irc7_ToString()));

        var here_user_guest = new UserProfile
        {
            Away = false,
            Level = EnumUserAccessLevel.Member,
            Guest = true
        };
        Assert.That("H,U,G", Is.EqualTo(here_user_guest.Irc7_ToString()));

        var away_user_male_prof_registered = new UserProfile
        {
            Away = true,
            Level = EnumUserAccessLevel.Member,
            Guest = false,
            HasProfile = true,
            IsMale = true,
            Registered = true
        };
        Assert.That("G,U,MX", Is.EqualTo(away_user_male_prof_registered.Irc7_ToString()));
    }
}