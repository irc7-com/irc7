using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Irc.Objects.User;
using Moq;

namespace Irc.Tests.Commands;

/// <summary>
/// Tests for the NAMES command (RPL_NAMEREPLY 353).
/// </summary>
[TestFixture]
public class NamesTests
{
    private Mock<IServer> _mockServer;

    [SetUp]
    public void SetUp()
    {
        _mockServer = new Mock<IServer>();
        _mockServer.Setup(s => s.ToString()).Returns("TestServer");
        _mockServer.Setup(s => s.Name).Returns("TestServer");
        _mockServer.Setup(s => s.MaxMessageLength).Returns(512);
    }

    /// <summary>
    /// When the full 353 reply fits within MaxMessageLength, exactly one 353 should be sent.
    /// </summary>
    [Test]
    public void ProcessNamesReply_SmallChannel_SendsSingle353()
    {
        var channel = new Channel("%#Lobby");

        var requesterUser = CreateMockUser("Sky");

        channel.Join(requesterUser.Object, EnumChannelAccessResult.NONE);

        var sentMessages = new List<string>();
        requesterUser.Setup(u => u.Send(It.IsAny<string>()))
            .Callback((string msg) => sentMessages.Add(msg));

        Names.ProcessNamesReply(requesterUser.Object, channel);

        var nameReplies = sentMessages.Where(m => m.Contains(" 353 ")).ToList();
        Assert.That(nameReplies.Count, Is.EqualTo(1), "Expected exactly one RPL_NAMEREPLY (353) line.");
    }

    /// <summary>
    /// When the names list would exceed MaxMessageLength, multiple 353 messages should be sent.
    /// </summary>
    [Test]
    public void ProcessNamesReply_ExceedsMaxMessageLength_SplitsIntoMultiple353s()
    {
        // Use a very small MaxMessageLength to force splitting.
        // Prefix for ":TestServer 353 Sky = %#Lobby :" = 1+10+5+3+1+1+1+7+2 = 31 chars
        // Setting MaxMessageLength to 40 means we have 40-2-31 = 7 chars for names.
        // "Sky" is 3 chars — fits as first name.
        // "UserA" is 5 chars — needs "Sky UserA" (9 chars), which exceeds 7, so splits.
        _mockServer.Setup(s => s.MaxMessageLength).Returns(40);

        var channel = new Channel("%#Lobby");

        var requesterUser = CreateMockUser("Sky");
        var memberUserA = CreateMockUser("UserA");
        var memberUserB = CreateMockUser("UserB");

        channel.Join(requesterUser.Object, EnumChannelAccessResult.NONE);
        channel.Join(memberUserA.Object, EnumChannelAccessResult.NONE);
        channel.Join(memberUserB.Object, EnumChannelAccessResult.NONE);

        var sentMessages = new List<string>();
        requesterUser.Setup(u => u.Send(It.IsAny<string>()))
            .Callback((string msg) => sentMessages.Add(msg));

        Names.ProcessNamesReply(requesterUser.Object, channel);

        var nameReplies = sentMessages.Where(m => m.Contains(" 353 ")).ToList();
        Assert.That(nameReplies.Count, Is.GreaterThan(1),
            "Expected multiple RPL_NAMEREPLY (353) lines when names exceed MaxMessageLength.");
    }

    /// <summary>
    /// Every name should appear in exactly one of the 353 reply messages.
    /// </summary>
    [Test]
    public void ProcessNamesReply_AllNamesPresent_AcrossAllReplies()
    {
        // Force split: small MaxMessageLength
        _mockServer.Setup(s => s.MaxMessageLength).Returns(40);

        var channel = new Channel("%#Lobby");

        var requesterUser = CreateMockUser("Sky");
        var memberUserA = CreateMockUser("UserA");
        var memberUserB = CreateMockUser("UserB");

        channel.Join(requesterUser.Object, EnumChannelAccessResult.NONE);
        channel.Join(memberUserA.Object, EnumChannelAccessResult.NONE);
        channel.Join(memberUserB.Object, EnumChannelAccessResult.NONE);

        var sentMessages = new List<string>();
        requesterUser.Setup(u => u.Send(It.IsAny<string>()))
            .Callback((string msg) => sentMessages.Add(msg));

        Names.ProcessNamesReply(requesterUser.Object, channel);

        var allNamesText = string.Join(' ', sentMessages.Where(m => m.Contains(" 353 ")));

        Assert.That(allNamesText, Does.Contain("Sky"), "Expected 'Sky' to appear in 353 replies.");
        Assert.That(allNamesText, Does.Contain("UserA"), "Expected 'UserA' to appear in 353 replies.");
        Assert.That(allNamesText, Does.Contain("UserB"), "Expected 'UserB' to appear in 353 replies.");
    }

    /// <summary>
    /// Each individual 353 message must not exceed MaxMessageLength bytes (excluding CRLF).
    /// </summary>
    [Test]
    public void ProcessNamesReply_Each353MessageDoesNotExceedMaxMessageLength()
    {
        // Use a small MaxMessageLength to trigger batching with many users.
        const int maxLen = 80;
        _mockServer.Setup(s => s.MaxMessageLength).Returns(maxLen);

        var channel = new Channel("%#Lobby");
        var requesterUser = CreateMockUser("Sky");
        channel.Join(requesterUser.Object, EnumChannelAccessResult.NONE);

        // Add enough users to force multiple batches.
        for (var i = 1; i <= 20; i++)
        {
            var member = CreateMockUser($"User{i:D2}");
            channel.Join(member.Object, EnumChannelAccessResult.NONE);
        }

        var sentMessages = new List<string>();
        requesterUser.Setup(u => u.Send(It.IsAny<string>()))
            .Callback((string msg) => sentMessages.Add(msg));

        Names.ProcessNamesReply(requesterUser.Object, channel);

        // Each 353 message (without the 2-byte CRLF) must fit within MaxMessageLength.
        foreach (var msg in sentMessages.Where(m => m.Contains(" 353 ")))
            Assert.That(msg.Length, Is.LessThanOrEqualTo(maxLen - 2),
                $"353 reply line exceeds MaxMessageLength - 2: '{msg}'");
    }

    /// <summary>
    /// ProcessNamesReply must always send an RPL_ENDOFNAMES (366) line.
    /// </summary>
    [Test]
    public void ProcessNamesReply_AlwaysSends366EndOfNames()
    {
        var channel = new Channel("%#Lobby");
        var requesterUser = CreateMockUser("Sky");
        channel.Join(requesterUser.Object, EnumChannelAccessResult.NONE);

        var sentMessages = new List<string>();
        requesterUser.Setup(u => u.Send(It.IsAny<string>()))
            .Callback((string msg) => sentMessages.Add(msg));

        Names.ProcessNamesReply(requesterUser.Object, channel);

        Assert.That(sentMessages.Any(m => m.Contains(" 366 ")), Is.True,
            "Expected RPL_ENDOFNAMES (366) to be sent.");
    }

    // -------------------------------------------------------------------------

    private Mock<IUser> CreateMockUser(string nickname)
    {
        var mockProtocol = new Mock<IProtocol>();
        mockProtocol.Setup(p => p.GetProtocolType()).Returns(EnumProtocolType.IRC8);
        // FormattedUser returns the nickname without any prefix for simplicity.
        mockProtocol.Setup(p => p.FormattedUser(It.IsAny<IChannelMember>()))
            .Returns<IChannelMember>(m => m.GetUser().ToString()!);

        var mockU = new Mock<IUser>();
        var address = new UserAddress();
        address.SetNickname(nickname);
        address.User = nickname;
        address.Host = "anon";
        address.RealName = nickname;

        mockU.Setup(u => u.GetAddress()).Returns(address);
        mockU.Setup(u => u.ToString()).Returns(nickname);
        mockU.Setup(u => u.Name).Returns(nickname);
        mockU.Setup(u => u.GetChannels()).Returns(new Dictionary<IChannel, IChannelMember>());
        mockU.Setup(u => u.Send(It.IsAny<string>()));
        mockU.Setup(u => u.GetLevel()).Returns(EnumUserAccessLevel.None);
        mockU.Setup(u => u.IsAdministrator()).Returns(false);
        mockU.Setup(u => u.GetProtocol()).Returns(mockProtocol.Object);
        mockU.Setup(u => u.Away).Returns(false);
        mockU.Setup(u => u.Modes).Returns(new UserModes());
        mockU.Setup(u => u.Server).Returns(_mockServer.Object);

        return mockU;
    }
}
