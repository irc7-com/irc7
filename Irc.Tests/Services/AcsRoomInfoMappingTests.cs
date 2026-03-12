using Irc.Services;

[TestFixture]
public class AcsRoomInfoMappingTests
{
    [Test]
    public void ToInMemoryChannel_ShouldMapAllFields_WhenValuesAreValid()
    {
        // Arrange
        var roomInfo = new AcsRoomInfo
        {
            ServerId = "server-01",
            Category = "GN",
            Name = "%#general",
            Topic = "General discussion",
            Modes = "+nt",
            Managed = true,
            Locale = "EN-US",
            Language = "2",
            CurrentUsers = 12,
            MaxUsers = 120
        };

        // Act
        var channel = roomInfo.ToInMemoryChannel();

        // Assert
        Assert.That(channel.ServerName, Is.EqualTo("server-01"));
        Assert.That(channel.Category, Is.EqualTo("GN"));
        Assert.That(channel.ChannelName, Is.EqualTo("%#general"));
        Assert.That(channel.ChannelTopic, Is.EqualTo("General discussion"));
        Assert.That(channel.Modes, Is.EqualTo("+nt"));
        Assert.That(channel.Locale, Is.EqualTo("EN-US"));
        Assert.That(channel.Language, Is.EqualTo(2));
        Assert.That(channel.UserLimit, Is.EqualTo(120));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("invalid")]
    public void ToInMemoryChannel_ShouldUseFallbacks_WhenLanguageOrMaxUsersAreInvalid(string? language)
    {
        // Arrange
        var roomInfo = new AcsRoomInfo
        {
            Name = "%#fallback",
            Language = language ?? string.Empty,
            MaxUsers = 0
        };

        // Act
        var channel = roomInfo.ToInMemoryChannel();

        // Assert
        Assert.That(channel.ChannelName, Is.EqualTo("%#fallback"));
        Assert.That(channel.Language, Is.EqualTo(1));
        Assert.That(channel.UserLimit, Is.EqualTo(50));
    }
}
