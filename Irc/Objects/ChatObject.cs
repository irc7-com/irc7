using Irc.Access;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Collections;

namespace Irc.Objects;

public class ChatObject : IChatObject
{
    protected readonly IModeCollection _modes;
    public readonly IDataStore DataStore;

    public ChatObject(IModeCollection modes, IDataStore dataStore)
    {
        _modes = modes;
        DataStore = dataStore;
        DataStore.SetId(Id.ToString());
    }

    public virtual EnumUserAccessLevel Level => EnumUserAccessLevel.None;

    public virtual IModeCollection Modes => _modes;

    public IModeCollection GetModes()
    {
        return _modes;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public string ShortId => Id.ToString().Split('-').Last();

    public string Name
    {
        get
        {
            var storeNick = DataStore.Get("Name");
            return string.IsNullOrWhiteSpace(storeNick) ? Resources.Wildcard : storeNick;
        }
        set => DataStore.Set("Name", value);
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

    public IPropCollection PropCollection { protected set; get; } = new PropCollection();
    public IAccessList AccessList { protected set; get; } = new AccessList();
}