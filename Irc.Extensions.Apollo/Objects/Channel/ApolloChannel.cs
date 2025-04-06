using Irc.Constants;
using Irc.Enumerations;
using Irc.Extensions.Objects.Channel;
using Irc.Interfaces;
using Irc.IO;
using Irc.Modes;
using Irc.Objects;

namespace Irc.Extensions.Apollo.Objects.Channel;

public class ApolloChannel : ExtendedChannel
{
    public ApolloChannel(string name, IChannelModes modeCollection, IDataStore dataStore) : base(name, modeCollection,
        dataStore)
    {
    }

    public override IChannel Join(IUser user, EnumChannelAccessResult accessResult = EnumChannelAccessResult.NONE)
    {
        var joinMember = AddMember(user, accessResult);
        foreach (var channelMember in GetMembers())
        {
            var channelUser = channelMember.GetUser();
            if (channelUser.GetProtocol().GetProtocolType() <= EnumProtocolType.IRC3)
            {
                channelMember.GetUser().Send(IrcRaws.RPL_JOIN(user, this));

                if (!joinMember.IsNormal())
                {
                    var modeChar = joinMember.IsOwner() ? 
                        Resources.MemberModeOwner : 
                        joinMember.IsHost() ? 
                            Resources.MemberModeHost : 
                            Resources.MemberModeVoice;
                    
                   ModeRule.DispatchModeChange((ChatObject)channelUser, modeChar,
                        (ChatObject)user, this, true, user.ToString());
                }
            }
            else
            {
                channelUser.Send(ApolloRaws.RPL_JOIN_MSN(channelMember, this, joinMember));
            }
        }

        return this;
    }
}