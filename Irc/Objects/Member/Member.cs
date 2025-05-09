﻿using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Modes.User;

namespace Irc.Objects.Member;

public class Member : MemberModes, IChannelMember
{
    protected readonly IUser _user;

    public Member(IUser User)
    {
        _user = User;
    }

    public EnumChannelAccessLevel GetLevel()
    {
        if (Owner.ModeValue)
            return EnumChannelAccessLevel.ChatOwner;

        if (Operator.ModeValue)
            return EnumChannelAccessLevel.ChatHost;

        if (Voice.ModeValue)
            return EnumChannelAccessLevel.ChatVoice;

        return EnumChannelAccessLevel.ChatMember;
    }

    public EnumIrcError CanModify(IChannelMember target, EnumChannelAccessLevel requiredLevel, bool operCheck = true)
    {
        if (operCheck)
            // Oper check
            if (target.GetUser().GetLevel() >= EnumUserAccessLevel.Guide)
            {
                if (_user.GetLevel() < EnumUserAccessLevel.Guide) return EnumIrcError.ERR_NOIRCOP;
                // TODO: Maybe there is better raws for below
                if (_user.GetLevel() < EnumUserAccessLevel.Sysop && _user.GetLevel() < target.GetUser().GetLevel())
                    return EnumIrcError.ERR_NOPERMS;
                if (_user.GetLevel() < EnumUserAccessLevel.Administrator &&
                    _user.GetLevel() < target.GetUser().GetLevel()) return EnumIrcError.ERR_NOPERMS;
            }

        var isOwner = Owner.ModeValue;
        var targetIsOwner = target.Owner.ModeValue;
        var isHost = Operator.ModeValue;

        if (!isOwner && (targetIsOwner || requiredLevel > EnumChannelAccessLevel.ChatHost))
            return EnumIrcError.ERR_NOCHANOWNER;
        if (!isOwner && !isHost && requiredLevel >= EnumChannelAccessLevel.ChatVoice) return EnumIrcError.ERR_NOCHANOP;

        return EnumIrcError.OK;
    }

    public IUser GetUser()
    {
        return _user;
    }
}