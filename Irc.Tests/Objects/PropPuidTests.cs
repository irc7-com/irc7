using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Irc.Objects.User;
using Moq;

namespace Irc.Tests.Objects;

[TestFixture]
public class PropPuidTests
{
    private PropPuid _prop = null!;
    private Mock<IServer> _mockServer = null!;

    [SetUp]
    public void SetUp()
    {
        _prop = new PropPuid();
        _mockServer = new Mock<IServer>();
        _mockServer.Setup(s => s.DisableGuestMode).Returns(false);
        _mockServer.Setup(s => s.ToString()).Returns("TestServer");
    }

    [Test]
    public void EvaluateGet_NonUserSource_ReturnsErrNoperms()
    {
        var mockSource = new Mock<IChatObject>();
        var target = CreateUser(guest: false, passport: false);

        var result = _prop.EvaluateGet(mockSource.Object, target);

        Assert.That(result, Is.EqualTo(EnumIrcError.ERR_NOPERMS));
    }

    [Test]
    public void EvaluateGet_NonUserTarget_ReturnsErrNoperms()
    {
        var source = CreateUser(guest: false, passport: false);
        var mockTarget = new Mock<IChatObject>();

        var result = _prop.EvaluateGet(source, mockTarget.Object);

        Assert.That(result, Is.EqualTo(EnumIrcError.ERR_NOPERMS));
    }

    [Test]
    public void EvaluateGet_GateFirst_PassportTarget_NoSharedChannel_ReturnsErrNoperms()
    {
        var source = CreateUser(guest: false, passport: false);
        var target = CreateUser(guest: false, passport: true, puid: "DEADBEEFDEADBEEF");

        var result = _prop.EvaluateGet(source, target);

        Assert.That(result, Is.EqualTo(EnumIrcError.ERR_NOPERMS),
            "Access gate must fire before PUID classification even for passport targets");
    }

    [Test]
    public void EvaluateGet_GateFirst_GuestTarget_NoSharedChannel_ReturnsErrNoperms()
    {
        var source = CreateUser(guest: false, passport: false);
        var target = CreateUser(guest: true, passport: false);

        var result = _prop.EvaluateGet(source, target);

        Assert.That(result, Is.EqualTo(EnumIrcError.ERR_NOPERMS),
            "Access gate must fire before guest classification");
    }

    [Test]
    public void EvaluateGet_CoMember_GuestTarget_ReturnsNoValue()
    {
        var source = CreateUser(guest: false, passport: false);
        var target = CreateUser(guest: true, passport: false);
        var channel = new Irc.Objects.Channel.Channel("#TestChannel");
        channel.Join(source);
        channel.Join(target);

        var result = _prop.EvaluateGet(source, target);

        Assert.That(result, Is.EqualTo(EnumIrcError.NO_VALUE));
    }

    [Test]
    public void EvaluateGet_CoMember_NullSspiHandler_ReturnsNoValue()
    {
        var source = CreateUser(guest: false, passport: false);
        var target = CreateUserNoSspi();
        var channel = new Irc.Objects.Channel.Channel("#TestChannel");
        channel.Join(source);
        channel.Join(target);

        var result = _prop.EvaluateGet(source, target);

        Assert.That(result, Is.EqualTo(EnumIrcError.NO_VALUE));
    }

    [Test]
    public void EvaluateGet_CoMember_NonPassportTarget_ReturnsNoValue()
    {
        var source = CreateUser(guest: false, passport: false);
        var target = CreateUser(guest: false, passport: false);
        var channel = new Irc.Objects.Channel.Channel("#TestChannel");
        channel.Join(source);
        channel.Join(target);

        var result = _prop.EvaluateGet(source, target);

        Assert.That(result, Is.EqualTo(EnumIrcError.NO_VALUE));
    }

    [Test]
    public void EvaluateGet_CoMember_PassportTarget_ReturnsOk()
    {
        var source = CreateUser(guest: false, passport: false);
        var target = CreateUser(guest: false, passport: true, puid: "F21F6EA24E994BAB");
        var channel = new Irc.Objects.Channel.Channel("#TestChannel");
        channel.Join(source);
        channel.Join(target);

        var result = _prop.EvaluateGet(source, target);

        Assert.That(result, Is.EqualTo(EnumIrcError.OK));
    }

    [Test]
    public void EvaluateGet_SelfQuery_PassportUser_ReturnsOk()
    {
        var user = CreateUser(guest: false, passport: true, puid: "SELFPUIDHEX01234");
        var channel = new Irc.Objects.Channel.Channel("#TestChannel");
        channel.Join(user);

        var result = _prop.EvaluateGet(user, user);

        Assert.That(result, Is.EqualTo(EnumIrcError.OK));
    }

    [Test]
    public void EvaluateGet_DifferentChannels_NoOverlap_ReturnsErrNoperms()
    {
        var source = CreateUser(guest: false, passport: true, puid: "SOURCEPUID");
        var target = CreateUser(guest: false, passport: true, puid: "TARGETPUID");
        var channelA = new Irc.Objects.Channel.Channel("#ChannelA");
        var channelB = new Irc.Objects.Channel.Channel("#ChannelB");
        channelA.Join(source);
        channelB.Join(target);

        var result = _prop.EvaluateGet(source, target);

        Assert.That(result, Is.EqualTo(EnumIrcError.ERR_NOPERMS));
    }

    [Test]
    public void FlagAndReply_NoPuidCoMember_IsGuest_FlagG_AndEvaluateGetIsNoValue()
    {
        var source = CreateUser(guest: false, passport: false);
        var target = CreateUser(guest: false, passport: false);
        var channel = new Irc.Objects.Channel.Channel("#TestChannel");
        channel.Join(source);
        channel.Join(target);

        var evaluateResult = _prop.EvaluateGet(source, target);

        Assert.That(target.GetProfile(), Is.Null,
            "A no-PUID target has no profile object");
        Assert.That(target.IsGuest(), Is.True,
            "A no-PUID target is a guest");
        Assert.That(target.GetFormattedProfile(EnumProtocolType.IRC8), Is.EqualTo("H,U,GO"),
            "No-PUID target must report gender 'G'");
        Assert.That(evaluateResult, Is.EqualTo(EnumIrcError.NO_VALUE),
            "No-PUID target must yield NO_VALUE (empty 819), not a denial");
    }

    [Test]
    public void FlagAndReply_PassportCoMember_NotGuest_FlagNotG_AndEvaluateGetIsOk()
    {
        var source = CreateUser(guest: false, passport: false);
        var target = CreateUser(guest: false, passport: true, puid: "F21F6EA24E994BAB");
        var channel = new Irc.Objects.Channel.Channel("#TestChannel");
        channel.Join(source);
        channel.Join(target);

        var evaluateResult = _prop.EvaluateGet(source, target);

        Assert.That(target.GetProfile(), Is.Not.Null,
            "A passport target has a profile object");
        Assert.That(target.IsGuest(), Is.False,
            "A passport target is not a guest");
        Assert.That(target.GetFormattedProfile(EnumProtocolType.IRC8), Is.EqualTo("H,U,RXO"),
            "Passport target must not produce the no-profile 'G' flag");
        Assert.That(evaluateResult, Is.EqualTo(EnumIrcError.OK),
            "Passport target must yield OK");
    }

    private Irc.Objects.User.User CreateUser(bool guest, bool passport, string puid = "")
    {
        var mockConnection = new Mock<IConnection>();
        var mockProtocol = new Mock<IProtocol>();
        var mockDataRegulator = new Mock<IDataRegulator>();
        var mockFloodProtectionProfile = new Mock<IFloodProtectionProfile>();
        var mockSaslHandler = new Mock<ISaslHandler>();
        var mockCredential = new Mock<ICredential>();

        mockConnection.Setup(x => x.GetIp()).Returns("127.0.0.1");
        mockProtocol.Setup(p => p.GetProtocolType()).Returns(EnumProtocolType.IRC8);
        mockDataRegulator.Setup(d => d.PushOutgoing(It.IsAny<string>())).Returns(0);
        mockSaslHandler.Setup(h => h.RequiresPassport).Returns(passport);
        mockCredential.Setup(c => c.Username).Returns(puid);
        mockSaslHandler.Setup(h => h.GetCredentials()).Returns(mockCredential.Object);

        var user = new Irc.Objects.User.User(
            mockConnection.Object,
            mockProtocol.Object,
            mockDataRegulator.Object,
            mockFloodProtectionProfile.Object,
            _mockServer.Object,
            _ => mockSaslHandler.Object)
        {
            Nickname = $"User_{Guid.NewGuid():N}"
        };

        user.InitializeSspiHandler(passport);
        if (!guest && passport) user.AssignPassportProfile();
        return user;
    }

    private Irc.Objects.User.User CreateUserNoSspi()
    {
        var mockConnection = new Mock<IConnection>();
        var mockProtocol = new Mock<IProtocol>();
        var mockDataRegulator = new Mock<IDataRegulator>();
        var mockFloodProtectionProfile = new Mock<IFloodProtectionProfile>();

        mockConnection.Setup(x => x.GetIp()).Returns("127.0.0.1");
        mockProtocol.Setup(p => p.GetProtocolType()).Returns(EnumProtocolType.IRC8);
        mockDataRegulator.Setup(d => d.PushOutgoing(It.IsAny<string>())).Returns(0);

        return new Irc.Objects.User.User(
            mockConnection.Object,
            mockProtocol.Object,
            mockDataRegulator.Object,
            mockFloodProtectionProfile.Object,
            _mockServer.Object,
            _ => throw new InvalidOperationException("SSPI factory must not be called for no-SSPI user"))
        {
            Nickname = $"NoSspiUser_{Guid.NewGuid():N}"
        };
    }
}
