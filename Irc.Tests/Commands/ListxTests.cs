using NUnit.Framework;
using Moq;
using Irc.Interfaces;
using Irc.Commands;
using Irc.Objects.Channel;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Irc;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Modes.Channel;
using Irc.Objects;

[TestFixture]
public class ListxTests
{
    private Mock<IServer> _mockServer;
    private Mock<IUser> _mockUser;
    private Mock<IChatFrame> _mockChatFrame;
    private List<IChannel> _mockChannels;

    [SetUp]
    public void SetUp()
    {
        _mockServer = new Mock<IServer>();
        _mockUser = new Mock<IUser>();
        _mockChatFrame = new Mock<IChatFrame>();
        _mockChannels = new List<IChannel>();

        // ユーザーのモック設定
        _mockUser.Setup(u => u.IsOn(It.IsAny<IChannel>())).Returns(true);
        _mockUser.Setup(u => u.Send(It.IsAny<string>()));
        _mockUser.Setup(u => u.GetLevel()).Returns(EnumUserAccessLevel.Guide);
        _mockUser.CallBase = true;
        _mockUser.Setup(s => s.ToString()).Returns("User1");
        
        // サーバーからチャンネルを取得するモック設定
        _mockServer.CallBase = true;
        _mockServer.Setup(s => s.ToString()).Returns("Server1");
        _mockServer.Setup(s => s.GetChannels()).Returns(_mockChannels);

        // チャットフレームのモック設定
        _mockChatFrame.Setup(c => c.Server).Returns(_mockServer.Object);
        _mockChatFrame.Setup(c => c.User).Returns(_mockUser.Object);
    }

    [Test]
    public void Execute_ShouldFilterChannelsByMemberCount()
    {
        // Arrange
        var channel1 = CreateMockChannel("Channel1", 5);
        var channel2 = CreateMockChannel("Channel2", 15);
        _mockChannels.AddRange(new[] { channel1, channel2 });

        var chatMessage = new Mock<IChatMessage>();
        chatMessage.Setup(m => m.Parameters).Returns(["<10"]);
        _mockChatFrame.Setup(c => c.ChatMessage).Returns(chatMessage.Object);

        var listx = new Listx();

        // Act
        listx.Execute(_mockChatFrame.Object);

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel1"))), Times.Once);
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel2"))), Times.Never);
    }

    [Test]
    public void Execute_ShouldFilterChannelsByNameMask()
    {
        // Arrange
        var channel1 = CreateMockChannel("TestChannel", 5);
        var channel2 = CreateMockChannel("OtherChannel", 15);
        _mockChannels.AddRange(new[] { channel1, channel2 });

        var chatMessage = new Mock<IChatMessage>();
        chatMessage.Setup(m => m.Parameters).Returns(["N=Test*" ]);
        _mockChatFrame.Setup(c => c.ChatMessage).Returns(chatMessage.Object);

        var listx = new Listx();

        // Act
        listx.Execute(_mockChatFrame.Object);

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("TestChannel"))), Times.Once);
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("OtherChannel"))), Times.Never);
    }
    
    [Test]
    public void Execute_ShouldFilterChannelsByRegisteredMode()
    {
        // Arrange
        var registeredChannel = CreateMockChannel("RegisteredChannel", 10, true);
        var unregisteredChannel = CreateMockChannel("UnregisteredChannel", 5, false);

        _mockChannels.AddRange(new[] { registeredChannel, unregisteredChannel });

        var chatMessage = new Mock<IChatMessage>();
        chatMessage.Setup(m => m.Parameters).Returns(["R=1"]);
        _mockChatFrame.Setup(c => c.ChatMessage).Returns(chatMessage.Object);

        var listx = new Listx();

        // Act
        listx.Execute(_mockChatFrame.Object);

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("RegisteredChannel"))), Times.Once);
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("UnregisteredChannel"))), Times.Never);
    }
    
    [Test]
    public void Execute_ShouldFilterChannelsBySubjectMask()
    {
        // Arrange
        var channel1 = CreateMockChannel("Channel1", 5, false,"Gaming and Fun");
        var channel2 = CreateMockChannel("Channel2", 10, false, "Programming Discussions");
        _mockChannels.AddRange(new[] { channel1, channel2 });

        var chatMessage = new Mock<IChatMessage>();
        chatMessage.Setup(m => m.Parameters).Returns(["S=*Gaming*"]);
        _mockChatFrame.Setup(c => c.ChatMessage).Returns(chatMessage.Object);

        var listx = new Listx();

        // Act
        listx.Execute(_mockChatFrame.Object);

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel1"))), Times.Once);
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel2"))), Times.Never);
    }
    
    [Test]
    public void Execute_ShouldFilterChannelsByTopicChangedLessThan()
    {
        // Arrange
        var epochNow = Resources.GetEpochNowInSeconds();
        var channel1 = CreateMockChannel(name: "Channel1", memberCount: 5, topicChanged: epochNow - 300); // 5分前
        var channel2 = CreateMockChannel(name: "Channel2", memberCount: 10, topicChanged: epochNow - 900); // 15分前
        _mockChannels.AddRange(new[] { channel1, channel2 });

        var chatMessage = new Mock<IChatMessage>();
        chatMessage.Setup(m => m.Parameters).Returns(["T<10"]); // 10分未満
        _mockChatFrame.Setup(c => c.ChatMessage).Returns(chatMessage.Object);

        var listx = new Listx();

        // Act
        listx.Execute(_mockChatFrame.Object);

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel1"))), Times.Once);
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel2"))), Times.Never);
    }
    
    [Test]
    public void Execute_ShouldFilterChannelsByCreationTimeLessThan()
    {
        // Arrange
        var epochNow = Resources.GetEpochNowInSeconds();
        var channel1 = CreateMockChannel(name: "Channel1", memberCount: 5, topicChanged: 0);
        var channel2 = CreateMockChannel(name: "Channel2", memberCount: 10, topicChanged: 0);

        Mock.Get(channel1).SetupGet(c => c.Creation).Returns(epochNow - 300); // 5分前
        Mock.Get(channel2).SetupGet(c => c.Creation).Returns(epochNow - 900); // 15分前

        _mockChannels.AddRange(new[] { channel1, channel2 });

        var chatMessage = new Mock<IChatMessage>();
        chatMessage.Setup(m => m.Parameters).Returns(["C<10"]); // 10分未満
        _mockChatFrame.Setup(c => c.ChatMessage).Returns(chatMessage.Object);

        var listx = new Listx();

        // Act
        listx.Execute(_mockChatFrame.Object);

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel1"))), Times.Once);
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel2"))), Times.Never);
    }
    
    [Test]
    public void Execute_ShouldFilterChannelsByCreationTimeGreaterThan()
    {
        // Arrange
        var epochNow = Resources.GetEpochNowInSeconds();
        var channel1 = CreateMockChannel(name: "Channel1", memberCount: 5, topicChanged: 0);
        var channel2 = CreateMockChannel(name: "Channel2", memberCount: 10, topicChanged: 0);

        Mock.Get(channel1).SetupGet(c => c.Creation).Returns(epochNow - 300); // 5分前
        Mock.Get(channel2).SetupGet(c => c.Creation).Returns(epochNow - 900); // 15分前

        _mockChannels.AddRange(new[] { channel1, channel2 });

        var chatMessage = new Mock<IChatMessage>();
        chatMessage.Setup(m => m.Parameters).Returns(["C>10"]); // 10分以上
        _mockChatFrame.Setup(c => c.ChatMessage).Returns(chatMessage.Object);

        var listx = new Listx();

        // Act
        listx.Execute(_mockChatFrame.Object);

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel2"))), Times.Once);
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel1"))), Times.Never);
    }
    
    [Test]
    public void Execute_ShouldFilterChannelsByTopicMask()
    {
        // Arrange
        var channel1 = CreateMockChannel(name: "Channel1", memberCount: 5, topicChanged: 0, topic: "Gaming and Fun");
        var channel2 = CreateMockChannel(name: "Channel2", memberCount: 10, topicChanged: 0, topic: "Programming Discussions");

        _mockChannels.AddRange(new[] { channel1, channel2 });

        var chatMessage = new Mock<IChatMessage>();
        chatMessage.Setup(m => m.Parameters).Returns(["T=*Gaming*"]);
        _mockChatFrame.Setup(c => c.ChatMessage).Returns(chatMessage.Object);

        var listx = new Listx();

        // Act
        listx.Execute(_mockChatFrame.Object);

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel1"))), Times.Once);
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel2"))), Times.Never);
    }
    
    [Test]
    public void Execute_ShouldFilterChannelsByTopicChangedGreaterThan()
    {
        // Arrange
        var epochNow = Resources.GetEpochNowInSeconds();
        var channel1 = CreateMockChannel(name: "Channel1", memberCount: 5, topicChanged: epochNow - 300); // 5分前
        var channel2 = CreateMockChannel(name: "Channel2", memberCount: 10, topicChanged: epochNow - 900); // 15分前
        _mockChannels.AddRange(new[] { channel1, channel2 });

        var chatMessage = new Mock<IChatMessage>();
        chatMessage.Setup(m => m.Parameters).Returns(["T>10"]); // 10分以上
        _mockChatFrame.Setup(c => c.ChatMessage).Returns(chatMessage.Object);

        var listx = new Listx();

        // Act
        listx.Execute(_mockChatFrame.Object);

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel2"))), Times.Once);
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("Channel1"))), Times.Never);
    }

    private IChannel CreateMockChannel(
        string name, 
        int memberCount, 
        bool registered = false, 
        string subject = "subject",
        string topic = "topic",
        long topicChanged = 0)
    {
        var mockChannelProps = new Mock<IChannelProps>();
        var mockChannelModes = new Mock<IChannelModes>();
        var mockChannelMembers = new Mock<IList<IChannelMember>>();
        var mockChannel = new Mock<IChannel>();

        mockChannel.SetupGet(c => c.TopicChanged).Returns(topicChanged);

        // Cannot get the below working
        mockChannel.As<IChatObject>().Setup(c => c.ToString()).Returns(name);

        mockChannel.Setup(c => c.Name).Returns(name);
        mockChannel.Setup(c => c.GetMembers()).Returns(new List<IChannelMember>(new IChannelMember[memberCount]));
        
        // チャンネルプロパティのモック設定
        mockChannelProps.SetupGet(p => p.Topic).Returns(new Topic { Value = topic });
        mockChannelProps.SetupGet(p => p.Subject).Returns(
            new PropRule(
                Resources.ChannelPropSubject, 
                EnumChannelAccessLevel.ChatMember,
                EnumChannelAccessLevel.None, 
                Resources.GenericProps, 
                string.Empty, 
                true
                )
            {
                Value = subject
            }
        );

        // チャンネルモードのモック設定
        mockChannelModes.SetupGet(m => m.Secret).Returns(new SecretRule());
        mockChannelModes.SetupGet(m => m.Private).Returns(new PrivateRule());
        mockChannelModes.SetupGet(m => m.UserLimit).Returns(new UserLimitRule { Value = 100 });
        mockChannelModes.SetupGet(m => m.Registered).Returns(new RegisteredRule() { ModeValue = registered });
        mockChannelModes.Setup(m => m.GetModeString()).Returns("+ntl 100");

        // チャンネルメンバーのモック設定
        mockChannelMembers.SetupGet(cm => cm.Count).Returns(memberCount);

        // チャンネルのモック設定
        mockChannel.SetupGet(c => c.Props).Returns(mockChannelProps.Object);
        mockChannel.SetupGet(c => c.Modes).Returns(mockChannelModes.Object);
        mockChannel.Setup(c => c.GetMembers()).Returns(mockChannelMembers.Object);
        return mockChannel.Object;
    }
}