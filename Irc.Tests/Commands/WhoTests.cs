using Irc.Commands;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Irc.Objects.User;
using Moq;

namespace Irc.Tests.Commands;

/// <summary>
/// Tests for the WHO command (RPL_WHOREPLY 352).
/// </summary>
[TestFixture]
public class WhoTests
{
    private Mock<IServer> _mockServer;

    [SetUp]
    public void SetUp()
    {
        _mockServer = new Mock<IServer>();
        _mockServer.Setup(s => s.ToString()).Returns("TestServer");
        _mockServer.Setup(s => s.Name).Returns("TestServer");
    }

    /// <summary>
    /// When WHO replies for channel members, each member's own channel mode should appear
    /// in the reply — not the requesting user's channel mode.
    /// Regression test for: WHO adds voice/owner mode of the requester to all members on raw 352.
    /// </summary>
    [Test]
    public void SendWho_ReportsEachMembersOwnChannelMode_NotRequestersMode()
    {
        // Arrange: channel with an owner (requester) and a regular member (target).
        var channel = new Channel("%#Lobby");

        var requesterUser = CreateMockUser("Sky", "Sky", "anon");
        var memberUser = CreateMockUser("Verzon", "Verzon", "anon");

        // Requester joins as owner; member joins with no elevated mode.
        channel.Join(requesterUser.Object, EnumChannelAccessResult.SUCCESS_OWNER);
        channel.Join(memberUser.Object, EnumChannelAccessResult.NONE);

        var requesterMember = channel.GetMember(requesterUser.Object)!;
        var memberMember = channel.GetMember(memberUser.Object)!;

        requesterUser.Setup(u => u.GetChannels())
            .Returns(new Dictionary<IChannel, IChannelMember> { { channel, requesterMember } });
        memberUser.Setup(u => u.GetChannels())
            .Returns(new Dictionary<IChannel, IChannelMember> { { channel, memberMember } });

        var sentMessages = new List<string>();
        requesterUser.Setup(u => u.Send(It.IsAny<string>()))
            .Callback((string msg) => sentMessages.Add(msg));

        // Act: requester issues WHO, receiving replies for the channel members.
        Who.SendWho(_mockServer.Object, requesterUser.Object, new List<IUser> { memberUser.Object }, true);

        // Assert: exactly one 352 reply was sent.
        var whoReply = sentMessages.SingleOrDefault(m => m.Contains(" 352 "));
        Assert.That(whoReply, Is.Not.Null, "Expected exactly one RPL_WHOREPLY (352) line.");

        // The owner mode char for the requester is 'q'.  It must NOT appear in the reply
        // for the regular member, because the member has no channel mode of their own.
        Assert.That(whoReply, Does.Not.Contain("q"),
            "Regular member's 352 reply must not carry the requester's owner mode 'q'.");
    }

    /// <summary>
    /// When the WHO target user is a channel owner, the owner mode char ('q') must appear
    /// in their own reply line.
    /// </summary>
    [Test]
    public void SendWho_ReportsOwnerModeForOwnerUser()
    {
        // Arrange: channel with two owners so we can verify mode is attached to the right user.
        var channel = new Channel("%#Lobby");

        var requesterUser = CreateMockUser("Sky", "Sky", "anon");
        var ownerMemberUser = CreateMockUser("Ver", "Ver", "anon");

        channel.Join(requesterUser.Object, EnumChannelAccessResult.NONE);
        channel.Join(ownerMemberUser.Object, EnumChannelAccessResult.SUCCESS_OWNER);

        var requesterMember = channel.GetMember(requesterUser.Object)!;
        var ownerMember = channel.GetMember(ownerMemberUser.Object)!;

        requesterUser.Setup(u => u.GetChannels())
            .Returns(new Dictionary<IChannel, IChannelMember> { { channel, requesterMember } });
        ownerMemberUser.Setup(u => u.GetChannels())
            .Returns(new Dictionary<IChannel, IChannelMember> { { channel, ownerMember } });

        var sentMessages = new List<string>();
        requesterUser.Setup(u => u.Send(It.IsAny<string>()))
            .Callback((string msg) => sentMessages.Add(msg));

        // Act
        Who.SendWho(_mockServer.Object, requesterUser.Object, new List<IUser> { ownerMemberUser.Object }, true);

        // Assert
        var whoReply = sentMessages.SingleOrDefault(m => m.Contains(" 352 "));
        Assert.That(whoReply, Is.Not.Null, "Expected exactly one RPL_WHOREPLY (352) line.");

        Assert.That(whoReply, Does.Contain("q"),
            "Owner member's 352 reply must contain the owner mode char 'q'.");
    }

    // -------------------------------------------------------------------------

    private Mock<IUser> CreateMockUser(string nickname, string username, string host)
    {
        var mockProtocol = new Mock<IProtocol>();
        mockProtocol.Setup(p => p.GetProtocolType()).Returns(EnumProtocolType.IRC4);
        mockProtocol.Setup(p => p.GetFormat(It.IsAny<IUser>())).Returns(nickname);

        var mockU = new Mock<IUser>();
        var address = new UserAddress();
        address.SetNickname(nickname);
        address.User = username;
        address.Host = host;
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
