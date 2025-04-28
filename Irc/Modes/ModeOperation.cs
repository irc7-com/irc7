using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;
using Irc.Objects;

namespace Irc.Modes;

public class ModeOperation
{
    public ModeOperation(IModeRule mode, IUser source, IChatObject target, bool modeFlag, string modeParameter)
    {
        Mode = mode;
        Source = source;
        Target = target;
        ModeFlag = modeFlag;
        ModeParameter = modeParameter;
    }

    public IModeRule Mode { get; set; }
    public IUser Source { get; set; }
    public IChatObject Target { get; set; }
    public bool ModeFlag { get; set; }
    public string ModeParameter { get; set; }

    public void Execute()
    {
        var result = Mode?.Evaluate((ChatObject)Source, Target, ModeFlag, ModeParameter);

        switch (result)
        {
            case EnumIrcError.ERR_NEEDMOREPARAMS:
            {
                // -> sky-8a15b323126 MODE #test +l hello
                // < - :sky - 8a15b323126 461 Sky MODE +l :Not enough parameters
                Source.Send(Raws.IRCX_ERR_NEEDMOREPARAMS_461(Source.Server, Source,
                    $"{Resources.CommandMode} {Mode?.GetModeChar()}"));
                break;
            }
            case EnumIrcError.ERR_NOCHANOP:
            {
                //:sky-8a15b323126 482 Sky3k #test :You're not channel operator
                Source.Send(Raws.IRCX_ERR_CHANOPRIVSNEEDED_482(Source.Server, Source, (IChannel)Target));
                break;
            }
            case EnumIrcError.ERR_NOCHANOWNER:
            {
                //:sky-8a15b323126 482 Sky3k #test :You're not channel operator
                Source.Send(Raws.IRCX_ERR_CHANQPRIVSNEEDED_485(Source.Server, Source, Target));
                break;
            }
            case EnumIrcError.ERR_NOIRCOP:
            {
                Source.Send(Raws.IRCX_ERR_NOPRIVILEGES_481(Source.Server, Source));
                break;
            }
            case EnumIrcError.ERR_NOTONCHANNEL:
            {
                Source.Send(Raws.IRCX_ERR_NOTONCHANNEL_442(Source.Server, Source, (IChannel)Target));
                break;
            }
            // TODO: The below should not happen
            case EnumIrcError.ERR_NOSUCHNICK:
            {
                Source.Send(Raws.IRCX_ERR_NOSUCHNICK_401(Source.Server, Source, Target.Name));
                break;
            }
            case EnumIrcError.ERR_NOSUCHCHANNEL:
            {
                Source.Send(Raws.IRCX_ERR_NOSUCHCHANNEL_403(Source.Server, Source, Target.Name));
                break;
            }
            case EnumIrcError.ERR_CANNOTSETFOROTHER:
            {
                Source.Send(Raws.IRCX_ERR_USERSDONTMATCH_502(Source.Server, Source));
                break;
            }
            case EnumIrcError.ERR_UNKNOWNMODEFLAG:
            {
                Source.Send(Raws.IRC_RAW_501(Source.Server, Source));
                break;
            }
            case EnumIrcError.ERR_NOPERMS:
            {
                Source.Send(Raws.IRCX_ERR_SECURITY_908(Source.Server, Source));
                break;
            }
            case EnumIrcError.ERR_KEYSET:
            {
                Source.Send(Raws.IRCX_ERR_KEYSET_467(Source.Server, Source, (IChannel)Target));
                break;
            }
        }
    }
}