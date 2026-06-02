using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Extensions;
using Irc.Interfaces;
using Irc.Objects;
using Irc.Objects.Channel;
using Irc.Objects.Member;
using Irc.Objects.User;
using Moq;

namespace Irc.Tests.Commands;

/// <summary>
/// Unit tests for the IRCv3 CAP capability-negotiation command and the
/// <c>multi-prefix</c> capability it gates.
/// </summary>
[TestFixture]
public class CapTests
{
    private Mock<IServer> _mockServer;
    private Mock<IUser> _mockUser;
    private Mock<IChatFrame> _mockChatFrame;
    private List<string> _sentMessages;
    private HashSet<string> _userCapabilities;

    [SetUp]
    public void SetUp()
    {
        _mockServer = new Mock<IServer>();
        _mockUser = new Mock<IUser>();
        _mockChatFrame = new Mock<IChatFrame>();
        _sentMessages = new List<string>();
        _userCapabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        _mockServer.Setup(s => s.ToString()).Returns("TestServer");

        // Capability tracking backed by a real HashSet
        _mockUser.SetupProperty(u => u.CapNegotiating, false);
        _mockUser.Setup(u => u.HasCapability(It.IsAny<string>()))
            .Returns((string cap) => _userCapabilities.Contains(cap));
        _mockUser.Setup(u => u.EnableCapability(It.IsAny<string>()))
            .Callback((string cap) => _userCapabilities.Add(cap));
        _mockUser.Setup(u => u.DisableCapability(It.IsAny<string>()))
            .Callback((string cap) => _userCapabilities.Remove(cap));
        _mockUser.Setup(u => u.GetCapabilities())
            .Returns(_userCapabilities);
        _mockUser.Setup(u => u.Send(It.IsAny<string>()))
            .Callback((string msg) => _sentMessages.Add(msg));
        _mockUser.Setup(u => u.ToString()).Returns("TestUser");

        var address = new UserAddress();
        address.SetNickname("TestUser");
        _mockUser.Setup(u => u.GetAddress()).Returns(address);

        _mockChatFrame.Setup(cf => cf.Server).Returns(_mockServer.Object);
        _mockChatFrame.Setup(cf => cf.User).Returns(_mockUser.Object);
    }

    // -----------------------------------------------------------------------
    // CAP LS
    // -----------------------------------------------------------------------

    [Test]
    public void CapLs_SendsAvailableCapabilities()
    {
        SetupChatMessage("LS");

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_sentMessages, Has.Count.EqualTo(1));
        Assert.That(_sentMessages[0], Does.Contain("CAP"));
        Assert.That(_sentMessages[0], Does.Contain("LS"));
        Assert.That(_sentMessages[0], Does.Contain(Resources.CapMultiPrefix));
    }

    [Test]
    public void CapLs_SetsCapNegotiatingTrue()
    {
        SetupChatMessage("LS");

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_mockUser.Object.CapNegotiating, Is.True);
    }

    // -----------------------------------------------------------------------
    // CAP LIST
    // -----------------------------------------------------------------------

    [Test]
    public void CapList_WhenNoCapabilitiesEnabled_SendsEmptyList()
    {
        SetupChatMessage("LIST");

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_sentMessages, Has.Count.EqualTo(1));
        Assert.That(_sentMessages[0], Does.Contain("CAP"));
        Assert.That(_sentMessages[0], Does.Contain("LIST"));
    }

    [Test]
    public void CapList_WhenMultiPrefixEnabled_IncludesItInResponse()
    {
        _userCapabilities.Add(Resources.CapMultiPrefix);
        SetupChatMessage("LIST");

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_sentMessages[0], Does.Contain(Resources.CapMultiPrefix));
    }

    // -----------------------------------------------------------------------
    // CAP REQ
    // -----------------------------------------------------------------------

    [Test]
    public void CapReq_SupportedCapability_SendsAck()
    {
        SetupChatMessage("REQ", Resources.CapMultiPrefix);

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_sentMessages, Has.Count.EqualTo(1));
        Assert.That(_sentMessages[0], Does.Contain("ACK"));
        Assert.That(_sentMessages[0], Does.Contain(Resources.CapMultiPrefix));
    }

    [Test]
    public void CapReq_SupportedCapability_EnablesCapability()
    {
        SetupChatMessage("REQ", Resources.CapMultiPrefix);

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_userCapabilities, Contains.Item(Resources.CapMultiPrefix));
    }

    [Test]
    public void CapReq_UnsupportedCapability_SendsNak()
    {
        SetupChatMessage("REQ", "unsupported-cap");

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_sentMessages, Has.Count.EqualTo(1));
        Assert.That(_sentMessages[0], Does.Contain("NAK"));
    }

    [Test]
    public void CapReq_UnsupportedCapability_DoesNotEnableAnyCapability()
    {
        SetupChatMessage("REQ", "unsupported-cap");

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_userCapabilities, Is.Empty);
    }

    [Test]
    public void CapReq_DisablePrefix_RemovesCapability()
    {
        _userCapabilities.Add(Resources.CapMultiPrefix);
        SetupChatMessage("REQ", $"-{Resources.CapMultiPrefix}");

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_sentMessages[0], Does.Contain("ACK"));
        Assert.That(_userCapabilities, Does.Not.Contain(Resources.CapMultiPrefix));
    }

    [Test]
    public void CapReq_MissingTrailingParameter_SendsNeedMoreParams()
    {
        SetupChatMessage("REQ"); // no second parameter

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_sentMessages[0], Does.Contain("461"));
    }

    // -----------------------------------------------------------------------
    // CAP END
    // -----------------------------------------------------------------------

    [Test]
    public void CapEnd_ClearsCapNegotiatingFlag()
    {
        _mockUser.Object.CapNegotiating = true;
        SetupChatMessage("END");

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_mockUser.Object.CapNegotiating, Is.False);
    }

    [Test]
    public void CapEnd_DoesNotSendAnyMessage()
    {
        SetupChatMessage("END");

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_sentMessages, Is.Empty);
    }

    // -----------------------------------------------------------------------
    // Unknown subcommand
    // -----------------------------------------------------------------------

    [Test]
    public void Cap_UnknownSubcommand_SendsNeedMoreParams()
    {
        SetupChatMessage("BADSUBCMD");

        new Cap().Execute(_mockChatFrame.Object);

        Assert.That(_sentMessages[0], Does.Contain("461"));
    }

    // -----------------------------------------------------------------------
    // CapabilityManager
    // -----------------------------------------------------------------------

    [Test]
    public void CapabilityManager_MultiPrefixIsSupported()
    {
        Assert.That(CapabilityManager.IsSupported(Resources.CapMultiPrefix), Is.True);
    }

    [Test]
    public void CapabilityManager_RandomCapIsNotSupported()
    {
        Assert.That(CapabilityManager.IsSupported("not-a-real-cap"), Is.False);
    }

    [Test]
    public void CapabilityManager_CapCheckIsCaseInsensitive()
    {
        Assert.That(CapabilityManager.IsSupported("MULTI-PREFIX"), Is.True);
    }

    // -----------------------------------------------------------------------
    // Registration deferral
    // -----------------------------------------------------------------------

    [Test]
    public void CanRegister_WhileCapNegotiating_ReturnsFalse()
    {
        var server = new Mock<IServer>();
        var user = new Mock<IUser>();
        var chatFrame = new Mock<IChatFrame>();

        server.Setup(s => s.DisableUserRegistration).Returns(false);

        var address = new UserAddress();
        address.SetNickname("TestUser");
        address.User = "test";
        address.Host = "host";
        address.Server = "TestServer";

        user.SetupProperty(u => u.CapNegotiating, true);
        user.Setup(u => u.IsAuthenticated()).Returns(false);
        user.Setup(u => u.IsAnon()).Returns(true);
        user.Setup(u => u.IsRegistered()).Returns(false);
        user.Setup(u => u.IsGuest()).Returns(false);
        user.Setup(u => u.GetLevel()).Returns(EnumUserAccessLevel.None);
        user.Setup(u => u.GetAddress()).Returns(address);

        chatFrame.Setup(cf => cf.Server).Returns(server.Object);
        chatFrame.Setup(cf => cf.User).Returns(user.Object);

        var canReg = Register.CanRegister(chatFrame.Object);

        Assert.That(canReg, Is.False, "Registration must be deferred while CapNegotiating is true.");
    }

    // -----------------------------------------------------------------------
    // multi-prefix in NAMES (353)
    // -----------------------------------------------------------------------

    [Test]
    public void ProcessNamesReply_WithoutMultiPrefix_ShowsHighestModeOnly()
    {
        var (user, channel, _) = BuildNamesScenario(multiPrefix: false, isOp: true, isVoiced: true);

        Names.ProcessNamesReply(user.Object, channel);

        // Should contain the op prefix but NOT the voice prefix
        var namesReply = GetNamesReply(user);
        Assert.That(namesReply, Does.Contain("@"));
        Assert.That(namesReply, Does.Not.Contain("@+"));
    }

    [Test]
    public void ProcessNamesReply_WithMultiPrefix_ShowsAllModes()
    {
        var (user, channel, _) = BuildNamesScenario(multiPrefix: true, isOp: true, isVoiced: true);

        Names.ProcessNamesReply(user.Object, channel);

        var namesReply = GetNamesReply(user);
        Assert.That(namesReply, Does.Contain("@+"),
            "353 reply should contain both @ (op) and + (voice) when multi-prefix is enabled.");
    }

    [Test]
    public void ProcessNamesReply_WithMultiPrefix_VoiceOnlyShowsSinglePrefix()
    {
        var (user, channel, _) = BuildNamesScenario(multiPrefix: true, isOp: false, isVoiced: true);

        Names.ProcessNamesReply(user.Object, channel);

        var namesReply = GetNamesReply(user);
        Assert.That(namesReply, Does.Contain("+"));
        Assert.That(namesReply, Does.Not.Contain("@+"));
    }

    [Test]
    public void ProcessNamesReply_WithMultiPrefix_NoModes_ShowsNoPrefix()
    {
        var (user, channel, _) = BuildNamesScenario(multiPrefix: true, isOp: false, isVoiced: false);

        Names.ProcessNamesReply(user.Object, channel);

        var namesReply = GetNamesReply(user);
        // Nickname should appear without any mode prefix
        Assert.That(namesReply, Does.Contain("MemberUser"));
        Assert.That(namesReply, Does.Not.Contain("@MemberUser"));
        Assert.That(namesReply, Does.Not.Contain("+MemberUser"));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private void SetupChatMessage(string subCommand, string? trailingParam = null)
    {
        var parameters = new List<string> { subCommand };
        if (trailingParam != null) parameters.Add(trailingParam);

        var mockMessage = new Mock<IChatMessage>();
        mockMessage.Setup(m => m.Parameters).Returns(parameters);
        _mockChatFrame.Setup(cf => cf.ChatMessage).Returns(mockMessage.Object);
    }

    private static string GetNamesReply(Mock<IUser> user)
    {
        var messages = new List<string>();
        user.Verify(u => u.Send(It.IsAny<string>()), Times.AtLeastOnce);
        user.Invocations
            .Where(inv => inv.Method.Name == "Send")
            .ToList()
            .ForEach(inv => messages.Add((string)inv.Arguments[0]));
        return messages.FirstOrDefault(m => m.Contains("353")) ?? string.Empty;
    }

    /// <summary>
    /// Builds a test scenario with a channel containing one member.
    /// </summary>
    private (Mock<IUser> User, Channel Channel, Member Member) BuildNamesScenario(
        bool multiPrefix, bool isOp, bool isVoiced)
    {
        var capabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (multiPrefix) capabilities.Add(Resources.CapMultiPrefix);

        var mockProtocol = new Mock<IProtocol>();
        mockProtocol.Setup(p => p.GetProtocolType()).Returns(EnumProtocolType.IRC4);
        mockProtocol.Setup(p => p.FormattedUser(It.IsAny<IChannelMember>()))
            .Returns((IChannelMember m) =>
            {
                var prefix = m.Owner.ModeValue ? "." : m.Operator.ModeValue ? "@" : m.Voice.ModeValue ? "+" : "";
                return $"{prefix}{m.GetUser().GetAddress().Nickname}";
            });

        var observerUser = new Mock<IUser>();
        var observerAddress = new UserAddress();
        observerAddress.SetNickname("Observer");
        observerUser.Setup(u => u.GetAddress()).Returns(observerAddress);
        observerUser.Setup(u => u.HasCapability(It.IsAny<string>()))
            .Returns((string cap) => capabilities.Contains(cap));
        observerUser.Setup(u => u.GetProtocol()).Returns(mockProtocol.Object);
        observerUser.Setup(u => u.Server).Returns(_mockServer.Object);
        var sentByObserver = new List<string>();
        observerUser.Setup(u => u.Send(It.IsAny<string>()))
            .Callback((string msg) => sentByObserver.Add(msg));

        // Rebuild mock to capture sent messages
        observerUser.Setup(u => u.Send(It.IsAny<string>()))
            .Callback((string msg) =>
            {
                sentByObserver.Add(msg);
                _sentMessages.Add(msg);
            });

        var memberMockUser = new Mock<IUser>();
        var memberAddress = new UserAddress();
        memberAddress.SetNickname("MemberUser");
        memberMockUser.Setup(u => u.GetAddress()).Returns(memberAddress);
        memberMockUser.Setup(u => u.GetProtocol()).Returns(mockProtocol.Object);
        memberMockUser.Setup(u => u.GetChannels()).Returns(new Dictionary<IChannel, IChannelMember>());
        memberMockUser.Setup(u => u.Send(It.IsAny<string>()));
        memberMockUser.Setup(u => u.GetLevel()).Returns(EnumUserAccessLevel.None);
        memberMockUser.Setup(u => u.IsAdministrator()).Returns(false);

        var channel = new Channel("%#test");
        var accessResult = isOp
            ? EnumChannelAccessResult.SUCCESS_HOST
            : EnumChannelAccessResult.SUCCESS_MEMBER;
        channel.Join(memberMockUser.Object, accessResult);

        var member = (Member)channel.GetMembers().First();
        if (isVoiced && !isOp) member.Voice.ModeValue = true;
        if (isVoiced && isOp) member.Voice.ModeValue = true;

        return (observerUser, channel, member);
    }
}
