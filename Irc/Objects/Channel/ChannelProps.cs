using System.Xml;
using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects.Collections;

namespace Irc.Objects.Channel;

// TODO: Further refactoring of the below to allow us to pass extended logic (e.g. via callbacks)
// Then remove the other classes from this file

public class Topic : PropRule
{
    public Topic() : base(Resources.ChannelPropTopic, EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.ChatHost, Resources.ChannelPropTopicRegex, string.Empty)
    {
    }

    public override string GetValue(IChatObject target)
    {
        return ((IChannel)target).Props.Topic.Value;
    }

    public override EnumIrcError EvaluateSet(IChatObject source, IChatObject target, string propValue)
    {
        var channel = (IChannel)target;

        var result = EnumIrcError.OK;
        if (channel.Modes.TopicOp)
        {
            result = base.EvaluateSet(source, target, propValue);
            if (result != EnumIrcError.OK) return result;   
        }

        channel.Props.Topic.Value = propValue;
        return result;
    }
}

public class Memberkey : PropRule
{
    // The MEMBERKEY channel property is the keyword required to enter the channel. The MEMBERKEY property is limited to 31 characters. 
    // It may never be read.
    public Memberkey() : base(Resources.ChannelPropMemberkey, EnumChannelAccessLevel.None,
        EnumChannelAccessLevel.ChatHost, Resources.GenericProps, string.Empty)
    {
    }

    public override EnumIrcError EvaluateSet(IChatObject source, IChatObject target, string propValue)
    {
        // MEMBERKEY being set to a value sends a prop reply but no MODE reply
        // Mode +k is enforced server-side however.

        // MEMBERKEY being set to blank sends a prop reply but no MODE reply
        // Mode -k is enforced server-side however

        var result = base.EvaluateSet(source, target, propValue);

        if (result == EnumIrcError.OK)
        {
            var channel = (IChannel)target;
            channel.Modes.Key = propValue;
        }

        return result;
    }
}

internal class ChannelProps : PropCollection, IChannelProps
{
    // limited to 200 bytes including 1 or 2 characters for channel prefix
    public PropRule Name { get; } = new(
        Resources.ChannelPropName, 
        EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.None, 
        Resources.GenericProps, 
        string.Empty, 
        true
    );

    public PropRule Creation { get; } = new(
        Resources.ChannelPropCreation,
        EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.None, 
        Resources.GenericProps, 
        Resources.GetEpochNowInSeconds().ToString(), 
        true
    );

    // The LANGUAGE channel property is the preferred language type. The LANGUAGE property is a string limited to 31 characters. 
    public PropRule Language { get; } = new(
        Resources.ChannelPropLanguage, 
        EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.ChatHost, 
        Resources.GenericProps, 
        string.Empty
    );

    // The OWNERKEY channel property is the owner keyword that will provide owner access when entering the channel. The OWNERKEY property is limited to 31 characters. 
    // It may never be read
    public PropRule OwnerKey { get; } = new(
        Resources.ChannelPropOwnerkey, 
        EnumChannelAccessLevel.None,
        EnumChannelAccessLevel.ChatOwner, 
        Resources.GenericProps, 
        string.Empty
    );

    // The HOSTKEY channel property is the host keyword that will provide host (channel op) access when entering the channel. 
    // It may never be read.
    public PropRule HostKey { get; } = new(
        Resources.ChannelPropHostkey, 
        EnumChannelAccessLevel.None,
        EnumChannelAccessLevel.ChatOwner, 
        Resources.GenericProps, 
        string.Empty
    );
    
    // MEMBERKEY
    public Memberkey MemberKey { get; } = new();
    
    // The PICS channel property is the current PICS rating of the channel.
    // Chat clients that are PICS enabled can use this property to determine if the channel is appropriate for the user.
    // The PICS property is limited to 255 characters.
    // This property may be set by sysop managers and read by all. It may not be read by ordinary users if the channel is SECRET.
    public PropRule Pics { get; } = new(
        Resources.ChannelPropPICS, EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.None, Resources.ChannelPropPICSRegex, string.Empty, true
    );
    
    // TOPIC
    public Topic Topic { get; } = new();
    
    // SUBJECT
    public PropRule Subject { get; } = new(
        Resources.ChannelPropSubject, 
        EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.None, 
        Resources.GenericProps, 
        string.Empty, 
        true
    );
    
    // The ONJOIN channel property contains a string to be sent (via PRIVMSG) to a user after the user has joined the channel.
    // The channel name is displayed as the sender of the message.
    // Only the user joining the channel will see this message.
    // Multiple lines can be generated by embedding '\n' in the string.
    // The ONJOIN property is limited to 255 characters.
    public PropRule Onjoin { get; } = new(
        Resources.ChannelPropOnJoin, 
        EnumChannelAccessLevel.ChatHost,
        EnumChannelAccessLevel.ChatHost, 
        Resources.ChannelPropOnjoinRegex, 
        string.Empty
    );

    // The ONPART channel property contains a string that is sent (via NOTICE) to a user after they have parted from the channel.
    // The channel name is displayed as the sender of the message. Only the user parting the channel will see message.
    // Multiple lines can be generated by embedding '\n' in the string. The ONPART property is limited to 255 characters.
    public PropRule Onpart { get; } = new(
        Resources.ChannelPropOnPart, 
        EnumChannelAccessLevel.ChatHost,
        EnumChannelAccessLevel.ChatHost, 
        Resources.ChannelPropOnpartRegex, 
        string.Empty
    );

    // The LAG channel property contains a numeric value between 0 to 2 seconds.
    // The server will add an artificial delay of that length between subsequent messages from the same member.
    // All messages to the channel are affected. 
    public PropRule Lag { get; } = new(
        Resources.ChannelPropLag, 
        EnumChannelAccessLevel.ChatHost,
        EnumChannelAccessLevel.ChatHost, 
        Resources.ChannelPropLagRegex, 
        string.Empty
    );
    
    // The CLIENT channel property contains client-specified information.
    // The format is not defined by the server.
    // The CLIENT property is limited to 255 characters.
    // This property may be set and read like the TOPIC property.
    public PropRule Client { get; } = new(
        Resources.ChannelPropClient, 
        EnumChannelAccessLevel.ChatMember,
        EnumChannelAccessLevel.ChatHost, 
        Resources.GenericProps, 
        string.Empty, 
        true
        );
    
    // The CLIENTGUID channel property contains a GUID that defines the client protocol to be used within the channel.
    // This property may be set and read like the LAG property. 
    public PropRule ClientGUID { get; } = new(
        Resources.ChannelPropClientGuid, 
        EnumChannelAccessLevel.None,
        EnumChannelAccessLevel.None,
        Resources.GenericProps, 
        string.Empty,
        true
        );

    public PropRule ServicePath { get; } = new(
        Resources.ChannelPropServicePath, 
        EnumChannelAccessLevel.None,
        EnumChannelAccessLevel.ChatOwner,
        Resources.GenericProps, 
        string.Empty, 
        true
        );
    
    // The ACCOUNT channel property contains an implementation-dependant string for attaching a security account.
    // This controls access to the channel using the native OS security system.
    // The ACCOUNT property is limited to 31 characters.
    // It can only be read by sysop managers, sysops and owners of the channel.
    public PropRule Account { get; } = new(
        Resources.ChannelPropAccount, 
        EnumChannelAccessLevel.ChatHost,
        EnumChannelAccessLevel.ChatHost, 
        Resources.GenericProps,
        string.Empty, 
        true
        );
    
    public ChannelProps()
    {
        AddProp(Oid);
        AddProp(Name);
        AddProp(Creation);
        AddProp(Language);
        AddProp(OwnerKey);
        AddProp(HostKey);
        AddProp(MemberKey);
        AddProp(Pics);
        AddProp(Topic);
        AddProp(Subject);
        AddProp(Onjoin);
        AddProp(Onpart);
        AddProp(Lag);
        AddProp(Client);
        AddProp(ClientGUID);
        AddProp(ServicePath);
        AddProp(Account);
    }
}