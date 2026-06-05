using Irc.Constants;
using Irc.Objects.User;

namespace Irc.Tests.Objects.User;

public class UserAddressTests
{
    [Test]
    public void Parse_NicknameOnlyGetsCorrectlyParsed()
    {
        var isValid = UserAddress.Parse("nickname", out var address);
        Assert.That(isValid, Is.True);
        Assert.That(address.Nickname, Is.EqualTo("nickname"));
        Assert.That(address.User, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.Host, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.Server, Is.EqualTo(Resources.Wildcard));
    }
    
    [Test]
    public void Parse_NicknameAndUserHost()
    {
        var isValid = UserAddress.Parse("nick!user", out var address);
        Assert.That(isValid, Is.True);
        Assert.That(address.Nickname, Is.EqualTo("nick"));
        Assert.That(address.User, Is.EqualTo("user"));
        Assert.That(address.Host, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.Server, Is.EqualTo(Resources.Wildcard));
    }

    [Test]
    public void Parse_NicknameUserHostHostname()
    {
        var isValid = UserAddress.Parse("nick!user@host", out var address);
        Assert.That(isValid, Is.True);
        Assert.That(address.Nickname, Is.EqualTo("nick"));
        Assert.That(address.User, Is.EqualTo("user"));
        Assert.That(address.Host, Is.EqualTo("host"));
        Assert.That(address.Server, Is.EqualTo(Resources.Wildcard));
    }

    [Test]
    public void Parse_FullAddress()
    {
        var isValid = UserAddress.Parse("nick!user@host$server", out var address);
        Assert.That(isValid, Is.True);
        Assert.That(address.Nickname, Is.EqualTo("nick"));
        Assert.That(address.User, Is.EqualTo("user"));
        Assert.That(address.Host, Is.EqualTo("host"));
        Assert.That(address.Server, Is.EqualTo("server"));
    }

    [Test]
    public void Parse_MissingNickname()
    {
        var isValid = UserAddress.Parse("!user", out var address);
        Assert.That(isValid, Is.True);
        Assert.That(address.Nickname, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.User, Is.EqualTo("user"));
    }

    [Test]
    public void Parse_MissingNicknameAndUserHost()
    {
        var isValid = UserAddress.Parse("@host", out var address);
        Assert.That(isValid, Is.True);
        Assert.That(address.Nickname, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.User, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.Host, Is.EqualTo("host"));
        Assert.That(address.Server, Is.EqualTo(Resources.Wildcard));
    }

    [Test]
    public void Parse_OnlyServer()
    {
        var isValid = UserAddress.Parse("$server", out var address);
        Assert.That(isValid, Is.True);
        Assert.That(address.Nickname, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.User, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.Host, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.Server, Is.EqualTo("server"));
    }

    [Test]
    public void Parse_NicknameHostname()
    {
        var isValid = UserAddress.Parse("nick@host", out var address);
        Assert.That(isValid, Is.True);
        Assert.That(address.Nickname, Is.EqualTo("nick"));
        Assert.That(address.User, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.Host, Is.EqualTo("host"));
        Assert.That(address.Server, Is.EqualTo(Resources.Wildcard));
    }

    [Test]
    public void Parse_NicknameServer()
    {
        var isValid = UserAddress.Parse("nick$server", out var address);
        Assert.That(isValid, Is.True);
        Assert.That(address.Nickname, Is.EqualTo("nick"));
        Assert.That(address.User, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.Host, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.Server, Is.EqualTo("server"));
    }

    [Test]
    public void Parse_Invalid_MultipleBang()
    {
        var isValid = UserAddress.Parse("nick!!user", out _);
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void Parse_Invalid_MultipleAt()
    {
        var isValid = UserAddress.Parse("nick@host@server", out _);
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void Parse_Invalid_MultipleDollar()
    {
        var isValid = UserAddress.Parse("nick$one$two", out _);
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void Parse_Invalid_WrongOrder_AtBeforeBang()
    {
        var isValid = UserAddress.Parse("nick@user!host", out _);
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void Parse_Invalid_WrongOrder_DollarBeforeAt()
    {
        var isValid = UserAddress.Parse("nick$host@server", out _);
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void Parse_Invalid_ForbiddenBangAtDollar()
    {
        var isValid = UserAddress.Parse("!@$", out _);
        Assert.That(isValid, Is.False);
    }
    
    [Test]
    public void Parse_EmptyPartsBetweenSeparators()
    {
        var isValid = UserAddress.Parse("nick!@host$", out var address);
        Assert.That(isValid, Is.True);
        Assert.That(address.Nickname, Is.EqualTo("nick"));
        Assert.That(address.User, Is.EqualTo(Resources.Wildcard));
        Assert.That(address.Host, Is.EqualTo("host"));
        Assert.That(address.Server, Is.EqualTo(Resources.Wildcard));
    }

    [Test]
    public void Parse_AllPartsEmptyExceptSeparators()
    {
        var isValid = UserAddress.Parse("!@", out var address);
        Assert.That(isValid, Is.False);
    }

    [Test]
    public void Parse_DollarBeforeBang()
    {
        var isValid = UserAddress.Parse("$!", out var address);
        Assert.That(isValid, Is.False);
    }
    
    [Test]
    public void Parse_DollarBeforeAt()
    {
        var isValid = UserAddress.Parse("$@", out var address);
        Assert.That(isValid, Is.False);
    }
    
    [Test]
    public void Parse_AtBeforeBang()
    {
        var isValid = UserAddress.Parse("@!", out var address);
        Assert.That(isValid, Is.False);
    }
    
    [Test]
    public void Parse_DollarAtBang()
    {
        var isValid = UserAddress.Parse("$@!", out var address);
        Assert.That(isValid, Is.False);
    }
    
    [Test]
    public void Parse_DollarDollarAt()
    {
        var isValid = UserAddress.Parse("$$@!", out var address);
        Assert.That(isValid, Is.False);
    }
    
    [Test]
    public void Parse_AtBangBang()
    {
        var isValid = UserAddress.Parse("@!!", out var address);
        Assert.That(isValid, Is.False);
    }
}