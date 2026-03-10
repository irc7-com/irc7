using Irc.Commands;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Irc.Objects.User;
using Moq;

namespace Irc.Tests.Commands;

/// <summary>
/// Tests for the IRCX CLONE feature (draft-pfenning-irc-extensions-04 sections 6.2, 8.1.16, 8.1.17).
/// </summary>
[TestFixture]
public class JoinCloneTests
{
    private Mock<IServer> _mockServer;
    private Mock<IUser> _mockUser;
    private List<string> _sentMessages;
    private List<IChannel> _serverChannels;

    [SetUp]
    public void SetUp()
    {
        _mockServer = new Mock<IServer>();
        _mockUser = new Mock<IUser>();
        _sentMessages = new List<string>();
        _serverChannels = new List<IChannel>();

        _mockServer.Setup(s => s.ToString()).Returns("MockServer");
        _mockServer.Setup(s => s.MaxChannels).Returns(10);
        _mockServer.Setup(s => s.JoinOnCreate).Returns(true);
        _mockServer.Setup(s => s.GetChannels()).Returns(_serverChannels);
        _mockServer.Setup(s => s.GetChannelByName(It.IsAny<string>()))
            .Returns((string name) =>
                _serverChannels.FirstOrDefault(c =>
                    string.Equals(c.GetName(), name, StringComparison.OrdinalIgnoreCase)));
        _mockServer.Setup(s => s.AddChannel(It.IsAny<IChannel>()))
            .Callback((IChannel ch) => _serverChannels.Add(ch))
            .Returns(true);
        _mockServer.Setup(s => s.RemoveChannel(It.IsAny<IChannel>()))
            .Callback((IChannel ch) => _serverChannels.Remove(ch));
        _mockServer.Setup(s => s.CreateChannel(It.IsAny<string>()))
            .Returns((string name) => new Channel(name));

        var mockProtocol = new Mock<IProtocol>();
        mockProtocol.Setup(p => p.GetProtocolType()).Returns(EnumProtocolType.IRC4);
        mockProtocol.Setup(p => p.GetFormat(It.IsAny<IUser>())).Returns("TestUser");

        _mockUser.Setup(u => u.Send(It.IsAny<string>()))
            .Callback((string msg) => _sentMessages.Add(msg));
        _mockUser.Setup(u => u.GetChannels())
            .Returns(new Dictionary<IChannel, IChannelMember>());
        _mockUser.Setup(u => u.GetLevel()).Returns(EnumUserAccessLevel.None);
        _mockUser.Setup(u => u.IsAdministrator()).Returns(false);
        _mockUser.Setup(u => u.ToString()).Returns("TestUser");
        _mockUser.Setup(u => u.GetProtocol()).Returns(mockProtocol.Object);

        var address = new UserAddress();
        address.SetNickname("TestUser");
        address.User = "test";
        address.Host = "host1";
        _mockUser.Setup(u => u.GetAddress()).Returns(address);
    }

    /// <summary>
    /// When a CLONEABLE channel is full, a user joining should be redirected to a new clone channel.
    /// </summary>
    [Test]
    public void JoinCloneableFullChannel_CreatesCloneAndJoinsUser()
    {
        // Arrange: create a CLONEABLE channel that is full (limit 1, 1 member already in)
        var parent = new Channel("%#chat");
        parent.Modes.Cloneable.ModeValue = true;
        parent.Modes.UserLimit.Value = 1;
        parent.Modes.NoExtern.ModeValue = true;
        parent.Modes.TopicOp.ModeValue = true;

        // Add a host member so they receive the CLONE notification
        var hostMessages = new List<string>();
        var hostUser = CreateMockUser("HostUser", "host", "host2");
        hostUser.Setup(u => u.Send(It.IsAny<string>())).Callback((string msg) => hostMessages.Add(msg));
        parent.Join(hostUser.Object, EnumChannelAccessResult.SUCCESS_HOST);
        _serverChannels.Add(parent);

        // Act
        Join.JoinChannels(_mockServer.Object, _mockUser.Object, new List<string> { "%#chat" }, string.Empty);

        // Assert: a clone channel was created
        var clone = _serverChannels.FirstOrDefault(c =>
            string.Equals(c.GetName(), "%#chat1", StringComparison.OrdinalIgnoreCase));
        Assert.That(clone, Is.Not.Null, "Clone channel '%#chat1' should have been created.");

        // Assert: clone has CLONE mode set
        Assert.That(clone!.Modes.Clone.ModeValue, Is.True, "Clone channel should have CLONE mode set.");

        // Assert: clone does NOT have CLONEABLE mode set
        Assert.That(clone.Modes.Cloneable.ModeValue, Is.False, "Clone channel should not inherit CLONEABLE mode.");

        // Assert: clone inherits user limit from parent
        Assert.That(clone.Modes.UserLimit.Value, Is.EqualTo(parent.Modes.UserLimit.Value),
            "Clone should inherit user limit from parent.");

        // Assert: CLONE message was sent to the host in the parent channel
        Assert.That(hostMessages.Any(m => m.Contains("CLONE") && m.Contains("%#chat") && m.Contains("%#chat1")),
            Is.True, "CLONE message should have been sent to parent channel hosts/owners.");
    }

    /// <summary>
    /// When a CLONEABLE channel is full but clone1 already exists and is not full,
    /// the user should join the existing clone without creating a new one.
    /// </summary>
    [Test]
    public void JoinCloneableFullChannel_ExistingCloneNotFull_JoinsExistingClone()
    {
        // Arrange: parent is full
        var parent = new Channel("%#chat");
        parent.Modes.Cloneable.ModeValue = true;
        parent.Modes.UserLimit.Value = 1;

        var existingUser = CreateMockUser("ExistingUser", "user2", "host2");
        parent.Join(existingUser.Object);
        _serverChannels.Add(parent);

        // Arrange: clone1 already exists and has room
        var existingClone = new Channel("%#chat1");
        existingClone.Modes.Clone.ModeValue = true;
        existingClone.Modes.UserLimit.Value = 10;
        _serverChannels.Add(existingClone);

        var channelCountBefore = _serverChannels.Count;

        // Act
        Join.JoinChannels(_mockServer.Object, _mockUser.Object, new List<string> { "%#chat" }, string.Empty);

        // Assert: no new channels were added (user joined existing clone)
        Assert.That(_serverChannels.Count, Is.EqualTo(channelCountBefore),
            "No new channel should be created when an existing clone has room.");

        // Assert: user is now in the existing clone
        Assert.That(existingClone.HasUser(_mockUser.Object), Is.True,
            "User should have joined the existing clone channel.");
    }

    /// <summary>
    /// When a CLONEABLE channel is full and clone1 is also full, clone2 should be created.
    /// </summary>
    [Test]
    public void JoinCloneableFullChannel_Clone1Full_CreatesClone2()
    {
        // Arrange: parent is full
        var parent = new Channel("%#chat");
        parent.Modes.Cloneable.ModeValue = true;
        parent.Modes.UserLimit.Value = 1;

        var existingUser = CreateMockUser("ExistingUser", "user2", "host2");
        parent.Join(existingUser.Object);
        _serverChannels.Add(parent);

        // Arrange: clone1 exists and is also full
        var clone1 = new Channel("%#chat1");
        clone1.Modes.Clone.ModeValue = true;
        clone1.Modes.UserLimit.Value = 1;

        var clone1User = CreateMockUser("Clone1User", "user3", "host3");
        clone1.Join(clone1User.Object);
        _serverChannels.Add(clone1);

        // Act
        Join.JoinChannels(_mockServer.Object, _mockUser.Object, new List<string> { "%#chat" }, string.Empty);

        // Assert: clone2 was created
        var clone2 = _serverChannels.FirstOrDefault(c =>
            string.Equals(c.GetName(), "%#chat2", StringComparison.OrdinalIgnoreCase));
        Assert.That(clone2, Is.Not.Null, "Clone channel '%#chat2' should have been created.");
        Assert.That(clone2!.Modes.Clone.ModeValue, Is.True, "Clone2 should have CLONE mode set.");
    }

    /// <summary>
    /// When a channel is full but does NOT have the CLONEABLE mode, the user should receive
    /// ERR_CHANNELISFULL without any clone being created.
    /// </summary>
    [Test]
    public void JoinFullChannel_NotCloneable_SendsChannelIsFullError()
    {
        // Arrange: full channel without CLONEABLE mode
        var parent = new Channel("%#chat");
        parent.Modes.UserLimit.Value = 1;
        parent.Modes.Cloneable.ModeValue = false;

        var existingUser = CreateMockUser("ExistingUser", "user2", "host2");
        parent.Join(existingUser.Object);
        _serverChannels.Add(parent);

        var initialChannelCount = _serverChannels.Count;

        // Act
        Join.JoinChannels(_mockServer.Object, _mockUser.Object, new List<string> { "%#chat" }, string.Empty);

        // Assert: ERR_CHANNELISFULL (471) was sent
        Assert.That(_sentMessages.Any(m => m.Contains("471")), Is.True,
            "ERR_CHANNELISFULL (471) should be sent when channel is full and not cloneable.");

        // Assert: no new clone channel was created
        Assert.That(_serverChannels.Count, Is.EqualTo(initialChannelCount),
            "No clone should be created for a non-cloneable channel.");
    }

    /// <summary>
    /// Clone channel inherits modes and props from the parent.
    /// </summary>
    [Test]
    public void CreateClone_InheritsModeFromParent()
    {
        // Arrange: parent with specific modes
        var parent = new Channel("%#modestest");
        parent.Modes.Cloneable.ModeValue = true;
        parent.Modes.UserLimit.Value = 2;
        parent.Modes.Moderated.ModeValue = true;
        parent.Modes.Secret.ModeValue = true;
        parent.Modes.NoExtern.ModeValue = true;
        parent.Modes.TopicOp.ModeValue = true;
        parent.Props.Topic.Value = "Test Topic";
        parent.Props.Category.Value = "GN";

        var existingUser1 = CreateMockUser("User1", "u1", "host1a");
        var existingUser2 = CreateMockUser("User2", "u2", "host2a");
        parent.Join(existingUser1.Object);
        parent.Join(existingUser2.Object);
        _serverChannels.Add(parent);

        // Act
        Join.JoinChannels(_mockServer.Object, _mockUser.Object, new List<string> { "%#modestest" }, string.Empty);

        // Assert: clone was created
        var clone = _serverChannels.FirstOrDefault(c =>
            string.Equals(c.GetName(), "%#modestest1", StringComparison.OrdinalIgnoreCase));
        Assert.That(clone, Is.Not.Null, "Clone channel should have been created.");

        // Assert: inherited modes
        Assert.That(clone!.Modes.Moderated.ModeValue, Is.True, "Clone should inherit Moderated mode.");
        Assert.That(clone.Modes.Secret.ModeValue, Is.True, "Clone should inherit Secret mode.");
        Assert.That(clone.Modes.NoExtern.ModeValue, Is.True, "Clone should inherit NoExtern mode.");
        Assert.That(clone.Modes.TopicOp.ModeValue, Is.True, "Clone should inherit TopicOp mode.");
        Assert.That(clone.Modes.UserLimit.Value, Is.EqualTo(2), "Clone should inherit UserLimit.");
        Assert.That(clone.Props.Topic.Value, Is.EqualTo("Test Topic"), "Clone should inherit topic.");
        Assert.That(clone.Props.Category.Value, Is.EqualTo("GN"), "Clone should inherit category.");

        // Assert: CLONE mode is set, CLONEABLE is not
        Assert.That(clone.Modes.Clone.ModeValue, Is.True, "Clone channel must have CLONE mode.");
        Assert.That(clone.Modes.Cloneable.ModeValue, Is.False, "Clone channel must not have CLONEABLE mode.");
    }

    /// <summary>
    /// CloneableRule should reject setting CLONEABLE mode on a channel whose name ends with a digit.
    /// </summary>
    [Test]
    public void CloneableRule_RejectsChannelNameEndingWithDigit()
    {
        // Arrange: use a real User (implements IChatObject) as required by the mode rule
        var user = CreateRealUser("TestUser");

        var channelWithDigit = new Channel("%#chat1");
        var cloneableRule = new CloneableRule();

        channelWithDigit.Join(user, EnumChannelAccessResult.SUCCESS_OWNER);

        // Act: try to set CLONEABLE on a channel ending with '1'
        var result = cloneableRule.Evaluate(user, channelWithDigit, true, string.Empty);

        // Assert
        Assert.That(result, Is.EqualTo(EnumIrcError.ERR_NOPERMS),
            "Setting CLONEABLE on a channel ending with a digit should return ERR_NOPERMS.");
    }

    /// <summary>
    /// CloneableRule should allow setting CLONEABLE mode on a channel whose name does not end with a digit.
    /// </summary>
    [Test]
    public void CloneableRule_AllowsChannelNameNotEndingWithDigit()
    {
        // Arrange
        var user = CreateRealUser("TestUser");

        var channel = new Channel("%#chat");
        var cloneableRule = new CloneableRule();

        channel.Join(user, EnumChannelAccessResult.SUCCESS_OWNER);

        // Act: try to set CLONEABLE on a channel not ending with a digit
        var result = cloneableRule.Evaluate(user, channel, true, string.Empty);

        // Assert: should succeed (not ERR_NOPERMS)
        Assert.That(result, Is.Not.EqualTo(EnumIrcError.ERR_NOPERMS),
            "Setting CLONEABLE on a channel not ending with a digit should not return ERR_NOPERMS.");
    }

    /// <summary>
    /// CloneRule should reject setting CLONE mode by a user without sysop privileges.
    /// </summary>
    [Test]
    public void CloneRule_RejectsNonSysopUser()
    {
        // Arrange
        var user = CreateRealUser("TestUser"); // default level is None (no sysop)

        var channel = new Channel("%#chat");
        var cloneRule = new CloneRule();
        channel.Join(user, EnumChannelAccessResult.SUCCESS_OWNER);

        // Act
        var result = cloneRule.Evaluate(user, channel, true, string.Empty);

        // Assert
        Assert.That(result, Is.EqualTo(EnumIrcError.ERR_NOPERMS),
            "Non-sysop users should not be able to set CLONE mode.");
    }

    /// <summary>
    /// CloneRule should allow setting CLONE mode by a sysop user.
    /// </summary>
    [Test]
    public void CloneRule_AllowsSysopUser()
    {
        // Arrange
        var user = CreateRealUser("SysopUser");
        user.SetLevel(EnumUserAccessLevel.Sysop);

        var channel = new Channel("%#chat");
        var cloneRule = new CloneRule();
        channel.Join(user, EnumChannelAccessResult.SUCCESS_OWNER);

        // Act
        var result = cloneRule.Evaluate(user, channel, true, string.Empty);

        // Assert: sysop should be allowed (result is OK)
        Assert.That(result, Is.EqualTo(EnumIrcError.OK),
            "Sysop users should be able to set CLONE mode.");
    }

    // Helper: creates a real User object (implements IChatObject) for use with mode rules.
    private Irc.Objects.User.User CreateRealUser(string nickname)
    {
        var mockConnection = new Mock<IConnection>();
        var mockProtocol = new Mock<IProtocol>();
        var mockDataRegulator = new Mock<IDataRegulator>();
        var mockFloodProtection = new Mock<IFloodProtectionProfile>();
        var mockSrv = new Mock<IServer>();
        mockConnection.Setup(c => c.GetIp()).Returns("127.0.0.1");

        var user = new Irc.Objects.User.User(
            mockConnection.Object, mockProtocol.Object,
            mockDataRegulator.Object, mockFloodProtection.Object, mockSrv.Object);
        user.Nickname = nickname;
        return user;
    }

    // Helper: creates a mock user with a unique address.
    private Mock<IUser> CreateMockUser(string nickname, string username, string host)
    {
        var mockProtocol = new Mock<IProtocol>();
        // Use IRC4+ to avoid the (ChatObject) cast in Channel.Join for modes dispatch
        mockProtocol.Setup(p => p.GetProtocolType()).Returns(EnumProtocolType.IRC4);
        mockProtocol.Setup(p => p.GetFormat(It.IsAny<IUser>())).Returns(nickname);

        var mockU = new Mock<IUser>();
        var address = new UserAddress();
        address.SetNickname(nickname);
        address.User = username;
        address.Host = host;
        mockU.Setup(u => u.GetAddress()).Returns(address);
        mockU.Setup(u => u.ToString()).Returns(nickname);
        mockU.Setup(u => u.GetChannels()).Returns(new Dictionary<IChannel, IChannelMember>());
        mockU.Setup(u => u.Send(It.IsAny<string>()));
        mockU.Setup(u => u.GetLevel()).Returns(EnumUserAccessLevel.None);
        mockU.Setup(u => u.IsAdministrator()).Returns(false);
        mockU.Setup(u => u.GetProtocol()).Returns(mockProtocol.Object);
        return mockU;
    }
}

