using Irc.Access;
using Irc.Enumerations;

namespace Irc.Tests.Objects;

[TestFixture]
public class AccessListTests
{
    [Test]
    public void GetEntries_PrunesExpiredEntries()
    {
        // Arrange
        var accessList = new Irc.Objects.Channel.ChannelAccess();
        var expiredEntry = new AccessEntry("tester@local", EnumUserAccessLevel.Member, EnumAccessLevel.DENY,
            "*!*@*", -1, "expired");
        var activeEntry = new AccessEntry("tester@local", EnumUserAccessLevel.Member, EnumAccessLevel.DENY,
            "*!*@*", 5, "active");

        accessList.Add(expiredEntry);
        accessList.Add(activeEntry);

        // Act
        var entries = accessList.GetEntries();

        // Assert
        Assert.That(entries.ContainsKey(EnumAccessLevel.DENY), Is.True);
        Assert.That(entries[EnumAccessLevel.DENY].Count, Is.EqualTo(1));
        Assert.That(entries[EnumAccessLevel.DENY].First().Reason, Is.EqualTo("active"));
    }
}
