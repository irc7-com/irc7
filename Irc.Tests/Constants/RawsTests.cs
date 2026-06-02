using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Irc.Objects.User;
using Moq;

namespace Irc.Tests.Constants;

[TestFixture]
public class RawsTests
{
    [Test]
    public void RplJoinMsn_Irc5AndAbove_UsesApolloProfileFormat()
    {
        var recipientProtocol = new Mock<IProtocol>();
        recipientProtocol.Setup(p => p.GetProtocolType()).Returns(EnumProtocolType.IRC5);
        recipientProtocol.Setup(p => p.GetFormat(It.IsAny<IUser>())).Returns("legacy");

        var recipientUser = new Mock<IUser>();
        recipientUser.Setup(u => u.GetProtocol()).Returns(recipientProtocol.Object);
        recipientUser.Setup(u => u.GetAddress()).Returns(new UserAddress());

        var recipientMember = new Mock<IChannelMember>();
        recipientMember.Setup(m => m.GetUser()).Returns(recipientUser.Object);

        var joinUser = CreateUser("JoinUser");
        joinUser.GetProfile().Registered = true;
        joinUser.GetProfile().HasProfile = false;
        joinUser.GetProfile().HasPicture = false;

        var joinMember = new Mock<IChannelMember>();
        joinMember.Setup(m => m.GetUser()).Returns(joinUser);
        joinMember.Setup(m => m.GetListedMode()).Returns(string.Empty);

        var raw = Raws.RPL_JOIN_MSN(recipientMember.Object, new Channel("%#chat"), joinMember.Object);

        Assert.That(raw, Does.Contain(" JOIN H,U,RBX :%#chat"));
    }

    [Test]
    public void CantKillServerRaw_Formats483Numeric()
    {
        var server = new Mock<IServer>();
        var user = new Mock<IUser>();
        server.Setup(s => s.ToString()).Returns("MockServer");
        user.Setup(u => u.ToString()).Returns("TestUser");

        var raw = Raws.IRCX_ERR_CANTKILLSERVER_483(server.Object, user.Object);

        Assert.That(raw, Does.Contain(" 483 "));
        Assert.That(raw, Does.Contain(":You can't kill a server!"));
    }

    private static User CreateUser(string nickname)
    {
        var mockConnection = new Mock<IConnection>();
        var mockProtocol = new Mock<IProtocol>();
        var mockDataRegulator = new Mock<IDataRegulator>();
        var mockFloodProtection = new Mock<IFloodProtectionProfile>();
        var mockServer = new Mock<IServer>();

        var address = new UserAddress();
        address.SetNickname(nickname);
        address.User = "user";
        address.Host = "host";
        mockConnection.Setup(c => c.GetIp()).Returns("127.0.0.1");

        var user = new User(
            mockConnection.Object,
            mockProtocol.Object,
            mockDataRegulator.Object,
            mockFloodProtection.Object,
            mockServer.Object);
        user.UserAddress = address;
        user.Nickname = nickname;
        return user;
    }
}
