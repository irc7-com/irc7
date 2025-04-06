using Irc.Extensions.Apollo.Objects.User;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Irc.Extensions.Apollo.Tests
{
    public class ApolloProfileTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ApolloProfileTests_GetProfileStringTests()
        {
            ApolloProfile fy = new ApolloProfile()
            {
                HasProfile = true,
                HasPicture = true,
                IsMale = false,
                IsFemale = true
            };

            Assert.That(13, Is.EqualTo(fy.GetProfileCode()));
            Assert.That("FY", Is.EqualTo(fy.GetProfileString()));

            ApolloProfile my = new ApolloProfile()
            {
                HasProfile = true,
                HasPicture = true,
                IsMale = true,
                IsFemale = false
            };

            Assert.That(11, Is.EqualTo(my.GetProfileCode()));
            Assert.That("MY", Is.EqualTo(my.GetProfileString()));

            ApolloProfile py = new ApolloProfile()
            {
                HasProfile = true,
                HasPicture = true,
                IsMale = false,
                IsFemale = false
            };

            Assert.That(9, Is.EqualTo(py.GetProfileCode()));
            Assert.That("PY", Is.EqualTo(py.GetProfileString()));

            ApolloProfile fx = new ApolloProfile()
            {
                HasProfile = true,
                HasPicture = false,
                IsMale = false,
                IsFemale = true
            };

            Assert.That(5, Is.EqualTo(fx.GetProfileCode()));
            Assert.That("FX", Is.EqualTo(fx.GetProfileString()));

            ApolloProfile mx = new ApolloProfile()
            {
                HasProfile = true,
                HasPicture = false,
                IsMale = true,
                IsFemale = false
            };

            Assert.That(3, Is.EqualTo(mx.GetProfileCode()));
            Assert.That("MX", Is.EqualTo(mx.GetProfileString()));

            ApolloProfile px = new ApolloProfile()
            {
                HasProfile = true,
                HasPicture = false,
                IsMale = false,
                IsFemale = false
            };

            Assert.That(1, Is.EqualTo(px.GetProfileCode()));
            Assert.That("PX", Is.EqualTo(px.GetProfileString()));

            ApolloProfile rx = new ApolloProfile()
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
            ApolloProfile admin = new ApolloProfile()
            {
                Level = Enumerations.EnumUserAccessLevel.Administrator
            };

            Assert.That("A", Is.EqualTo(admin.GetModeString()));

            ApolloProfile sysop = new ApolloProfile()
            {
                Level = Enumerations.EnumUserAccessLevel.Sysop
            };

            Assert.That("S", Is.EqualTo(sysop.GetModeString()));

            ApolloProfile user = new ApolloProfile()
            {
                Level = Enumerations.EnumUserAccessLevel.Member
            };

            Assert.That("U", Is.EqualTo(user.GetModeString()));
        }

        [Test]
        public void ApolloProfileTests_GetAwayStringTests()
        {
            ApolloProfile gone = new ApolloProfile()
            {
                Away = true
            };
            Assert.That("G", Is.EqualTo(gone.GetAwayString()));

            ApolloProfile here = new ApolloProfile()
            {
                Away = false
            };
            Assert.That("H", Is.EqualTo(here.GetAwayString()));
        }

        [Test]
        public void ApolloProfileTests_ToString()
        {
            ApolloProfile here_admin_guest = new ApolloProfile()
            {
                Away = false,
                Level = Enumerations.EnumUserAccessLevel.Administrator,
                Guest = true
            };
            Assert.That("H,A,GO", Is.EqualTo(here_admin_guest.ToString()));

            ApolloProfile here_user_guest = new ApolloProfile()
            {
                Away = false,
                Level = Enumerations.EnumUserAccessLevel.Member,
                Guest = true
            };
            Assert.That("H,U,GO", Is.EqualTo(here_user_guest.ToString()));

            ApolloProfile away_user_male_prof_registered = new ApolloProfile()
            {
                Away = true,
                Level = Enumerations.EnumUserAccessLevel.Member,
                Guest = false,
                HasProfile = true,
                IsMale = true,
                Registered = true
            };
            Assert.That("G,U,MXB", Is.EqualTo(away_user_male_prof_registered.ToString()));

            ApolloProfile away_user_female_prof_pic_registered = new ApolloProfile()
            {
                Away = true,
                Level = Enumerations.EnumUserAccessLevel.Member,
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
            ApolloProfile here_admin_guest = new ApolloProfile()
            {
                Away = false,
                Level = Enumerations.EnumUserAccessLevel.Administrator,
                Guest = true
            };
            Assert.That("H,A,G", Is.EqualTo(here_admin_guest.Irc5_ToString()));

            ApolloProfile here_user_guest = new ApolloProfile()
            {
                Away = false,
                Level = Enumerations.EnumUserAccessLevel.Member,
                Guest = true
            };
            Assert.That("H,U,G", Is.EqualTo(here_user_guest.Irc5_ToString()));

            ApolloProfile away_user_male_prof_registered = new ApolloProfile()
            {
                Away = true,
                Level = Enumerations.EnumUserAccessLevel.Member,
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
            ApolloProfile here_admin_guest = new ApolloProfile()
            {
                Away = false,
                Level = Enumerations.EnumUserAccessLevel.Administrator,
                Guest = true
            };
            Assert.That("H,A,G", Is.EqualTo(here_admin_guest.Irc7_ToString()));

            ApolloProfile here_user_guest = new ApolloProfile()
            {
                Away = false,
                Level = Enumerations.EnumUserAccessLevel.Member,
                Guest = true
            };
            Assert.That("H,U,G", Is.EqualTo(here_user_guest.Irc7_ToString()));

            ApolloProfile away_user_male_prof_registered = new ApolloProfile()
            {
                Away = true,
                Level = Enumerations.EnumUserAccessLevel.Member,
                Guest = false,
                HasProfile = true,
                IsMale = true,
                Registered = true
            };
            Assert.That("G,U,MX", Is.EqualTo(away_user_male_prof_registered.Irc7_ToString()));
        }
    }
}
