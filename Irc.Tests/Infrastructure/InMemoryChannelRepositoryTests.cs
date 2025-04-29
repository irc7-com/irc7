using System;
using Irc.Infrastructure;
using Irc.Objects.Channel;

[TestFixture]
public class InMemoryChannelRepositoryTests
{
    [Test]
    public void AddChannel_ShouldAddChannel_WhenValid()
    {
        // Arrange
        var channel = new InMemoryChannel
        {
            Category = "GN",
            ChannelName = "testChannel",
            ChannelTopic = "Test Channel",
            Modes = "-",
            Region = "EN-US",
            Language = "1",
            OwnerKey = "1234",
            Unknown = 0,
        };

        // Act
        InMemoryChannelRepository.Add(channel);

        // Assert
        var retrievedChannel = InMemoryChannelRepository.GetByName("testChannel");

        Assert.That(retrievedChannel, Is.Not.Null);
        Assert.That(retrievedChannel!.ChannelName, Is.EqualTo(channel.ChannelName));

        InMemoryChannelRepository.Remove("testChannel");
    }

    [Test]
    public void GetAllChannels_ShouldReturnAllChannels()
    {
        // Arrange
        var channel1 = new InMemoryChannel { ChannelName = "testChannel1" };
        var channel2 = new InMemoryChannel { ChannelName = "testChannel2" };

        InMemoryChannelRepository.Add(channel1);
        InMemoryChannelRepository.Add(channel2);

        // Act
        var channels = InMemoryChannelRepository.GetAllChannels();

        // Assert
        Assert.That(channels.ToList(), Has.Count.EqualTo(2));
        Assert.That(channels, Does.Contain(channel1));
        Assert.That(channels, Does.Contain(channel2));

        InMemoryChannelRepository.Remove("testChannel1");
        InMemoryChannelRepository.Remove("testChannel2");
    }

    [Test]
    public void RemoveChannel_ShouldRemoveChannel_WhenExists()
    {
        // Arrange
        var channel = new InMemoryChannel { ChannelName = "testChannel" };

        InMemoryChannelRepository.Add(channel);

        // Act
        InMemoryChannelRepository.Remove("testChannel");

        // Assert
        var retrievedChannel = InMemoryChannelRepository.GetByName("testChannel");
        Assert.That(retrievedChannel, Is.Null);
    }
}
