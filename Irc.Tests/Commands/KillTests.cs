using Irc.Commands;
using Irc.Interfaces;
using Irc.Objects.User;
using Moq;

namespace Irc.Tests.Commands;

[TestFixture]
public class KillTests
{
    private Mock<IServer> _mockServer = null!;
    private Mock<IUser> _mockAdmin = null!;
    private Mock<IChatFrame> _mockChatFrame = null!;
    private Mock<IChatMessage> _mockChatMessage = null!;
    private Dictionary<IChannel, IChannelMember> _adminChannels = null!;

    [SetUp]
    public void SetUp()
    {
        _mockServer = new Mock<IServer>();
        _mockAdmin = new Mock<IUser>();
        _mockChatFrame = new Mock<IChatFrame>();
        _mockChatMessage = new Mock<IChatMessage>();
        _adminChannels = new Dictionary<IChannel, IChannelMember>();

        var adminAddress = new Mock<IUserAddress>();
        adminAddress.SetupGet(a => a.RemoteIp).Returns("127.0.0.1");
        adminAddress.SetupGet(a => a.User).Returns("user");
        adminAddress.SetupGet(a => a.Nickname).Returns("Administrator");
        adminAddress.SetupGet(a => a.Host).Returns("localhost");

        _mockAdmin.Setup(u => u.GetLevel()).Returns(Enumerations.EnumUserAccessLevel.Sysop);
        _mockAdmin.Setup(u => u.GetAddress()).Returns(adminAddress.Object);
        _mockAdmin.Setup(u => u.GetChannels()).Returns(_adminChannels);
        _mockAdmin.Setup(u => u.ToString()).Returns("Administrator");

        _mockServer.Setup(s => s.ToString()).Returns("MockServer");

        _mockChatMessage.Setup(m => m.Parameters).Returns(new List<string> { "DupNick", "Test reason" });

        _mockChatFrame.Setup(f => f.Server).Returns(_mockServer.Object);
        _mockChatFrame.Setup(f => f.User).Returns(_mockAdmin.Object);
        _mockChatFrame.Setup(f => f.ChatMessage).Returns(_mockChatMessage.Object);
    }

    [Test]
    public void Execute_TargetNickNotOnServer_SendsNoSuchNick()
    {
        _mockServer.Setup(s => s.GetUsers()).Returns(new List<IUser> { CreateMockUser("OtherNick").Object });

        var kill = new Kill();
        kill.Execute(_mockChatFrame.Object);

        _mockAdmin.Verify(u => u.Send(It.Is<string>(raw => raw.Contains(" 401 "))), Times.Once);
    }

    [Test]
    public void Execute_AdminNotInChannel_KillsMatchingUsersServerWide()
    {
        var firstTarget = CreateMockUser("DupNick");
        var secondTarget = CreateMockUser("DupNick");
        _mockServer.Setup(s => s.GetUsers()).Returns(new List<IUser> { firstTarget.Object, secondTarget.Object });

        var disconnectCount = 0;
        firstTarget.Setup(u => u.Disconnect(It.IsAny<string>())).Callback(() => disconnectCount++);
        secondTarget.Setup(u => u.Disconnect(It.IsAny<string>())).Callback(() => disconnectCount++);

        var kill = new Kill();
        kill.Execute(_mockChatFrame.Object);

        Assert.That(disconnectCount, Is.EqualTo(2));
    }

    [Test]
    public void Execute_TargetNickOnServer_KillsAllMatchingNicknameConnections()
    {
        var sourceTarget = CreateMockUser("DupNick");

        var duplicateTarget = CreateMockUser("DupNick");
        var otherUser = CreateMockUser("OtherNick");

        _mockServer.Setup(s => s.GetUsers()).Returns(new List<IUser>
        {
            sourceTarget.Object,
            duplicateTarget.Object,
            otherUser.Object
        });

        var disconnectCount = 0;
        sourceTarget.Setup(u => u.Disconnect(It.IsAny<string>())).Callback(() => disconnectCount++);
        duplicateTarget.Setup(u => u.Disconnect(It.IsAny<string>())).Callback(() => disconnectCount++);
        otherUser.Setup(u => u.Disconnect(It.IsAny<string>())).Callback(() => disconnectCount++);

        var kill = new Kill();
        kill.Execute(_mockChatFrame.Object);

        Assert.That(disconnectCount, Is.EqualTo(2), "All matching users should be disconnected.");
        otherUser.Verify(u => u.Disconnect(It.IsAny<string>()), Times.Never);
    }

    private static Mock<IUser> CreateMockUser(string nickname)
    {
        var user = new Mock<IUser>();
        var channels = new Dictionary<IChannel, IChannelMember>();

        var address = new Mock<IUserAddress>();
        address.SetupGet(a => a.RemoteIp).Returns("127.0.0.1");
        address.SetupGet(a => a.User).Returns("user");
        address.SetupGet(a => a.Nickname).Returns(nickname);
        address.SetupGet(a => a.Host).Returns("localhost");

        user.Setup(u => u.GetAddress()).Returns(address.Object);
        user.Setup(u => u.GetLevel()).Returns(Enumerations.EnumUserAccessLevel.None);
        user.Setup(u => u.GetChannels()).Returns(channels);
        user.Setup(u => u.RemoveChannel(It.IsAny<IChannel>()))
            .Callback((IChannel channel) => channels.Remove(channel));
        user.Setup(u => u.ToString()).Returns(nickname);

        var targetChannel = new Mock<IChannel>();
        var targetMember = new Mock<IChannelMember>();
        targetMember.Setup(m => m.GetUser()).Returns(user.Object);
        var members = new List<IChannelMember> { targetMember.Object };
        targetChannel.Setup(c => c.GetMembers()).Returns(members);
        targetChannel.Setup(c => c.ToString()).Returns("%#test");

        channels[targetChannel.Object] = targetMember.Object;

        return user;
    }
}
