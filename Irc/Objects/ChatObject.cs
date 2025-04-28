using Irc.Access;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Collections;

namespace Irc.Objects;

public class ChatObject : IChatObject
{
    public virtual EnumUserAccessLevel Level => EnumUserAccessLevel.None;

    public virtual IModeCollection Modes { protected set; get; } = new ModeCollection();
    public virtual IPropCollection Props { protected set; get; } = new PropCollection();
    public virtual IAccessList Access { protected set; get; } = new AccessList();

    public Guid Id { get; } = Guid.NewGuid();

    public string ShortId => Id.ToString().Split('-').Last();
    
    private string _name = string.Empty;

    public string Name
    {
        get => string.IsNullOrWhiteSpace(_name) ? Resources.Wildcard : _name;
        set => _name = value;
    }

    public virtual void Send(string message)
    {
        throw new NotImplementedException();
    }

    public virtual void Send(string message, ChatObject except)
    {
        throw new NotImplementedException();
    }

    public virtual void Send(string message, EnumChannelAccessLevel accessLevel)
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return Name;
    }

    public virtual bool CanBeModifiedBy(IChatObject source)
    {
        throw new NotImplementedException();
    }

}