using System;
using Irc.Commands;
using Irc.Infrastructure;
using Irc.Interfaces;
using Irc.Objects.Channel;
using Moq;

[TestFixture]
public class CreateTests
{
    private Mock<IServer> _mockServer;
    private Mock<IUser> _mockUser;
    private Mock<IChatFrame> _mockChatFrame;

    [SetUp]
    public void SetUp()
    {
        _mockServer = new Mock<IServer>();
        _mockUser = new Mock<IUser>();
        _mockChatFrame = new Mock<IChatFrame>();

        _mockServer.Setup(s => s.ToString()).Returns("MockServer");
        _mockServer.Setup(s => s.GetSupportedChannelModes()).Returns("SWabdefghiklmnopqrstuvwxz");
        _mockServer.CallBase = true;

        _mockUser.Setup(u => u.Send(It.IsAny<string>()));
        _mockUser.CallBase = true;

        _mockChatFrame.Setup(cf => cf.Server).Returns(_mockServer.Object);
        _mockChatFrame.Setup(cf => cf.User).Returns(_mockUser.Object);

        _mockServer.Setup(s => s.CreateChannel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(new Mock<IChannel>().Object);
    }

    [Test]
    public void TestCreateCommand_ValidParameters_CreatesChannel()
    {
        // Arrange
        var parameters = new List<string>
        {
            "GN",
            $@"%#test\b1",
            "%ChannelTopic",
            "-",
            "EN-US",
            "1",
            "1234",
            "0",
        };

        _mockChatFrame.Setup(cf => cf.ChatMessage.Parameters).Returns(parameters);

        var createCommand = new Create();

        // Act
        try
        {
            createCommand.Execute(_mockChatFrame.Object);
        }
        catch (NullReferenceException)
        {
        }

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("613"))), Times.Once);

        InMemoryChannelRepository.Remove(parameters[1]);
    }

    [Test]
    public void TestCreateCommand_InvalidCategory_ReturnsError()
    {
        // Arrange
        var parameters = new List<string>
        {
            "INVALID_CATEGORY",
            $@"%#test\b1",
            "%ChannelTopic",
            "-",
            "EN-US",
            "1",
            "1234",
            "0",
        };

        _mockChatFrame.Setup(cf => cf.ChatMessage.Parameters).Returns(parameters);

        var createCommand = new Create();

        // Act
        try
        {
            createCommand.Execute(_mockChatFrame.Object);
        }
        catch (NullReferenceException)
        {
        }

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("701"))), Times.Once);
        _mockUser.Verify(
            u => u.Send(It.Is<string>(s => s.Contains("Category not found"))),
            Times.Once
        );
    }

    [Test]
    public void TestCreateCommand_InvalidChannelName_ReturnsError()
    {
        // Arrange
        var parameters = new List<string>
        {
            "GN",
            "INVALID_CHANNEL_NAME",
            "%ChannelTopic",
            "-",
            "EN-US",
            "1",
            "1234",
            "0",
        };

        _mockChatFrame.Setup(cf => cf.ChatMessage.Parameters).Returns(parameters);

        var createCommand = new Create();

        // Act
        try
        {
            createCommand.Execute(_mockChatFrame.Object);
        }
        catch (NullReferenceException)
        {
        }

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("706"))), Times.Once);
        _mockUser.Verify(
            u => u.Send(It.Is<string>(s => s.Contains("Channel name is not valid"))),
            Times.Once
        );
    }

    [Test]
    public void TestCreateCommand_InvalidRegion_ReturnsError()
    {
        // Arrange
        var parameters = new List<string>
        {
            "GN",
            $@"%#test\b1",
            "%ChannelTopic",
            "-",
            "INVALID_REGION",
            "1",
            "1234",
            "0",
        };

        _mockChatFrame.Setup(cf => cf.ChatMessage.Parameters).Returns(parameters);

        var createCommand = new Create();

        // Act
        try
        {
            createCommand.Execute(_mockChatFrame.Object);
        }
        catch (NullReferenceException)
        {
        }

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("706"))), Times.Once);
        _mockUser.Verify(
            u => u.Send(It.Is<string>(s => s.Contains("Locale parameter is not valid"))),
            Times.Once
        );
    }

    [Test]
    public void TestCreateCommand_ChannelAlreadyExists_ReturnsError()
    {
        // Arrange
        var parameters = new List<string>
        {
            "GN",
            $@"%#test\b1",
            "%ChannelTopic",
            "-",
            "EN-US",
            "1",
            "1234",
            "0",
        };

        _mockChatFrame.Setup(cf => cf.ChatMessage.Parameters).Returns(parameters);
        _mockServer.Setup(s => s.CreateChannel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns((IChannel)null);

        var createCommand = new Create();

        // Simulate that the channel already exists
        InMemoryChannelRepository.Add(
            new InMemoryChannel { ChannelName = $@"%#test\b1" }
        );

        // Act
        try
        {
            createCommand.Execute(_mockChatFrame.Object);
        }
        catch (NullReferenceException)
        {
        }

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("705"))), Times.Once);
        InMemoryChannelRepository.Remove($@"%#test\b1");
    }

    [Test]
    public void TestCreateCommand_UnsupportedMode_ReturnsError()
    {
        // Arrange
        var parameters = new List<string>
        {
            "GN",
            $@"%#test\b1",
            "%ChannelTopic",
            "INVALID_MODE",
            "EN-US",
            "1",
            "1234",
            "0",
        };

        _mockChatFrame.Setup(cf => cf.ChatMessage.Parameters).Returns(parameters);

        var createCommand = new Create();

        // Act
        try
        {
            createCommand.Execute(_mockChatFrame.Object);
        }
        catch (NullReferenceException)
        {
        }

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("706"))), Times.Once);
        _mockUser.Verify(
            u => u.Send(It.Is<string>(s => s.Contains("Channel mode is not valid"))),
            Times.Once
        );
    }

    [Test]
    public void TestCreateCommand_InvalidLRSID_ReturnsError()
    {
        // Arrange
        var parameters = new List<string>
        {
            "GN",
            $@"%#test\b1",
            "%ChannelTopic",
            "-",
            "EN-US",
            "1",
            "1234",
            "abcd",
        };

        _mockChatFrame.Setup(cf => cf.ChatMessage.Parameters).Returns(parameters);

        var createCommand = new Create();

        // Act
        try
        {
            createCommand.Execute(_mockChatFrame.Object);
        }
        catch (NullReferenceException)
        {
        }

        // Assert
        _mockUser.Verify(u => u.Send(It.Is<string>(s => s.Contains("706"))), Times.Once);
    }
}
