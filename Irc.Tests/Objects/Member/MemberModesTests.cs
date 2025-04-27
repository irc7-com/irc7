using Irc.Objects.Member;

namespace Irc.Tests.Objects.Member;

[TestFixture]
public class MemberModesTests
{
    [Test]
    public void HasModes_AllFalse_ReturnsFalse()
    {
        var memberModes = new MemberModes();

        Assert.That(memberModes.HasModes(), Is.False);
    }

    [Test]
    public void HasModes_OnlyOwnerTrue_ReturnsTrue()
    {
        var memberModes = new MemberModes();
        memberModes.Owner.ModeValue = true;

        Assert.That(memberModes.HasModes(), Is.True);
    }

    [Test]
    public void HasModes_OnlyOperatorTrue_ReturnsTrue()
    {
        var memberModes = new MemberModes();
        memberModes.Operator.ModeValue = true;

        Assert.That(memberModes.HasModes(), Is.True);
    }

    [Test]
    public void HasModes_OnlyVoiceTrue_ReturnsTrue()
    {
        var memberModes = new MemberModes();
        memberModes.Voice.ModeValue = true;

        Assert.That(memberModes.HasModes(), Is.True);
    }

    [Test]
    public void HasModes_OwnerAndOperatorTrue_ReturnsTrue()
    {
        var memberModes = new MemberModes();
        memberModes.Owner.ModeValue = true;
        memberModes.Operator.ModeValue = true;

        Assert.That(memberModes.HasModes(), Is.True);
    }

    [Test]
    public void HasModes_OwnerAndVoiceTrue_ReturnsTrue()
    {
        var memberModes = new MemberModes();
        memberModes.Owner.ModeValue = true;
        memberModes.Voice.ModeValue = true;

        Assert.That(memberModes.HasModes(), Is.True);
    }

    [Test]
    public void HasModes_OperatorAndVoiceTrue_ReturnsTrue()
    {
        var memberModes = new MemberModes();
        memberModes.Operator.ModeValue = true;
        memberModes.Voice.ModeValue = true;

        Assert.That(memberModes.HasModes(), Is.True);
    }

    [Test]
    public void HasModes_AllTrue_ReturnsTrue()
    {
        var memberModes = new MemberModes();
        memberModes.Owner.ModeValue = true;
        memberModes.Operator.ModeValue = true;
        memberModes.Voice.ModeValue = true;

        Assert.That(memberModes.HasModes(), Is.True);
    }
}