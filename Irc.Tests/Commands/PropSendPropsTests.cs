using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;
using Irc.Objects.Channel;
using Irc.Objects.User;
using Moq;

namespace Irc.Tests.Commands;

[TestFixture]
public class PropSendPropsTests
{
    private Prop _propCommand = null!;
    private PropPuid _puidProp = null!;
    private Mock<IServer> _mockServer = null!;

    [SetUp]
    public void SetUp()
    {
        _propCommand = new Prop();
        _puidProp = new PropPuid();
        _mockServer = new Mock<IServer>();
        _mockServer.Setup(s => s.DisableGuestMode).Returns(false);
        _mockServer.Setup(s => s.ToString()).Returns("TestServer");
        _mockServer.Setup(s => s.Name).Returns("TestServer");
    }

    [Test]
    public void SendProps_NoPuidCoMember_Sends819Only()
    {
        var capturedMessages = new List<string>();
        var source = CreateUser("Source", guest: false, passport: false,
            capturedMessages: capturedMessages);
        var target = CreateUser("Target", guest: false, passport: false);

        var channel = new Channel("#TestChannel");
        channel.Join(source);
        channel.Join(target);

        _propCommand.SendProps(_mockServer.Object, source, target,
            new List<IPropRule> { _puidProp });

        var count819 = capturedMessages.Count(m => m.Contains(" 819 "));
        var count818 = capturedMessages.Count(m => m.Contains(" 818 "));
        var count908 = capturedMessages.Count(m => m.Contains(" 908 "));

        Assert.That(count819, Is.EqualTo(1), "Expected exactly one IRCX_RPL_PROPEND_819");
        Assert.That(count818, Is.EqualTo(0), "Expected zero IRCX_RPL_PROPLIST_818 (no PUID)");
        Assert.That(count908, Is.EqualTo(0), "Expected zero IRCX_ERR_SECURITY_908 (SC-184-01: no-PUID returns empty, not denial)");
    }

    [Test]
    public void SendProps_NonCoMember_SingleProp_Sends908Only()
    {
        var capturedMessages = new List<string>();
        var source = CreateUser("Source", guest: false, passport: false,
            capturedMessages: capturedMessages);
        var target = CreateUser("Target", guest: false, passport: true, puid: "DEADBEEF01234567");

        _propCommand.SendProps(_mockServer.Object, source, target,
            new List<IPropRule> { _puidProp });

        var count908 = capturedMessages.Count(m => m.Contains(" 908 "));
        var count818 = capturedMessages.Count(m => m.Contains(" 818 "));
        var count819 = capturedMessages.Count(m => m.Contains(" 819 "));

        Assert.That(count908, Is.EqualTo(1), "Expected exactly one IRCX_ERR_SECURITY_908 for non-co-member");
        Assert.That(count818, Is.EqualTo(0), "Expected zero IRCX_RPL_PROPLIST_818");
        Assert.That(count819, Is.EqualTo(0), "Expected zero IRCX_RPL_PROPEND_819 (no emptyProp, no propsSent)");
    }

    [Test]
    public void SendProps_PassportCoMember_Sends818WithPuidThen819()
    {
        const string puid = "F21F6EA24E994BAB";
        var capturedMessages = new List<string>();
        var source = CreateUser("Source", guest: false, passport: false,
            capturedMessages: capturedMessages);
        var target = CreateUser("Target", guest: false, passport: true, puid: puid);

        var channel = new Channel("#TestChannel");
        channel.Join(source);
        channel.Join(target);

        _propCommand.SendProps(_mockServer.Object, source, target,
            new List<IPropRule> { _puidProp });

        var msg818 = capturedMessages.Where(m => m.Contains(" 818 ")).ToList();
        var count819 = capturedMessages.Count(m => m.Contains(" 819 "));
        var count908 = capturedMessages.Count(m => m.Contains(" 908 "));

        Assert.That(msg818, Has.Count.EqualTo(1), "Expected exactly one IRCX_RPL_PROPLIST_818");
        Assert.That(msg818[0], Does.Contain("PUID"), "818 message must name the PUID property");
        Assert.That(msg818[0], Does.Contain(puid),
            $"818 message must carry the PUID value '{puid}'");
        Assert.That(count819, Is.EqualTo(1), "Expected exactly one IRCX_RPL_PROPEND_819 terminator");
        Assert.That(count908, Is.EqualTo(0), "Expected zero 908 for a co-member passport query");
    }

    [Test]
    public void SendProps_GuestCoMember_Sends819Only()
    {
        var capturedMessages = new List<string>();
        var source = CreateUser("Source", guest: false, passport: false,
            capturedMessages: capturedMessages);
        var target = CreateUser("Target", guest: true, passport: false);

        var channel = new Channel("#TestChannel");
        channel.Join(source);
        channel.Join(target);

        _propCommand.SendProps(_mockServer.Object, source, target,
            new List<IPropRule> { _puidProp });

        var count819 = capturedMessages.Count(m => m.Contains(" 819 "));
        var count818 = capturedMessages.Count(m => m.Contains(" 818 "));
        var count908 = capturedMessages.Count(m => m.Contains(" 908 "));

        Assert.That(count819, Is.EqualTo(1), "Guest target must yield empty property list (819)");
        Assert.That(count818, Is.EqualTo(0), "No 818 for guest target");
        Assert.That(count908, Is.EqualTo(0), "No 908 for guest co-member (SC-184-01)");
    }

    [Test]
    public void SendProps_NonCoMember_MultipleProps_Sends_No908()
    {
        var capturedMessages = new List<string>();
        var source = CreateUser("Source", guest: false, passport: false,
            capturedMessages: capturedMessages);
        var target = CreateUser("Target", guest: false, passport: true, puid: "AABBCCDD11223344");

        _propCommand.SendProps(_mockServer.Object, source, target,
            new List<IPropRule> { _puidProp, _puidProp });

        var count908 = capturedMessages.Count(m => m.Contains(" 908 "));

        Assert.That(count908, Is.EqualTo(0),
            "908 must only be emitted when props.Count==1 (single-prop permission denial)");
    }

    [Test]
    public void SendProps_DeterministicForRepeatedCalls()
    {
        var captured1 = new List<string>();
        var captured2 = new List<string>();

        var source1 = CreateUser("Src1", guest: false, passport: false, capturedMessages: captured1);
        var target1 = CreateUser("Tgt1", guest: false, passport: false);
        var channel1 = new Channel("#Chan1");
        channel1.Join(source1);
        channel1.Join(target1);

        var source2 = CreateUser("Src2", guest: false, passport: false, capturedMessages: captured2);
        var target2 = CreateUser("Tgt2", guest: false, passport: false);
        var channel2 = new Channel("#Chan2");
        channel2.Join(source2);
        channel2.Join(target2);

        _propCommand.SendProps(_mockServer.Object, source1, target1,
            new List<IPropRule> { _puidProp });
        _propCommand.SendProps(_mockServer.Object, source2, target2,
            new List<IPropRule> { _puidProp });

        var count819_1 = captured1.Count(m => m.Contains(" 819 "));
        var count819_2 = captured2.Count(m => m.Contains(" 819 "));
        var count818_1 = captured1.Count(m => m.Contains(" 818 "));
        var count818_2 = captured2.Count(m => m.Contains(" 818 "));

        Assert.That(count819_1, Is.EqualTo(count819_2), "819 counts must be identical across runs");
        Assert.That(count818_1, Is.EqualTo(count818_2), "818 counts must be identical across runs");
    }

    private User CreateUser(
        string nickname,
        bool guest,
        bool passport,
        string puid = "",
        List<string>? capturedMessages = null)
    {
        var mockConnection = new Mock<IConnection>();
        var mockProtocol = new Mock<IProtocol>();
        var mockDataRegulator = new Mock<IDataRegulator>();
        var mockFloodProtectionProfile = new Mock<IFloodProtectionProfile>();
        var mockSaslHandler = new Mock<ISaslHandler>();
        var mockCredential = new Mock<ICredential>();

        mockConnection.Setup(x => x.GetIp()).Returns("127.0.0.1");
        mockProtocol.Setup(p => p.GetProtocolType()).Returns(EnumProtocolType.IRC8);
        mockSaslHandler.Setup(h => h.RequiresPassport).Returns(passport);
        mockCredential.Setup(c => c.Username).Returns(puid);
        mockSaslHandler.Setup(h => h.GetCredentials()).Returns(mockCredential.Object);

        if (capturedMessages != null)
        {
            mockDataRegulator
                .Setup(d => d.PushOutgoing(It.IsAny<string>()))
                .Callback<string>(capturedMessages.Add)
                .Returns(0);
        }
        else
        {
            mockDataRegulator.Setup(d => d.PushOutgoing(It.IsAny<string>())).Returns(0);
        }

        var user = new User(
            mockConnection.Object,
            mockProtocol.Object,
            mockDataRegulator.Object,
            mockFloodProtectionProfile.Object,
            _mockServer.Object,
            _ => mockSaslHandler.Object)
        {
            Nickname = nickname
        };

        user.SetGuest(guest);
        user.InitializeSspiHandler(passport);
        return user;
    }
}
