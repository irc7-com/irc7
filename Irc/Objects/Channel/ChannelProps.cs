using Irc.Objects.Collections;
using Irc.Props.Channel;

namespace Irc.Objects.Channel;

internal class ChannelProps : PropCollection
{
    public ChannelProps()
    {
        AddProp(new OID());
        AddProp(new Name());
        AddProp(new Creation());
        AddProp(new Language());
        AddProp(new Ownerkey());
        AddProp(new Hostkey());
        AddProp(new Memberkey());
        AddProp(new Pics());
        AddProp(new Topic());
        AddProp(new Subject());
        AddProp(new Onjoin());
        AddProp(new Onpart());
        AddProp(new Lag());
        AddProp(new Client());
        AddProp(new ClientGUID());
        AddProp(new ServicePath());
    }
}