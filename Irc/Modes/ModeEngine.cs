using Irc.Constants;
using Irc.Objects;

namespace Irc.Modes;

public class ModeEngine
{
    private readonly IModeCollection modeCollection;
    private readonly Dictionary<char, ModeRule> modeRules = new();

    public ModeEngine(IModeCollection modeCollection)
    {
        this.modeCollection = modeCollection;
    }

    public void AddModeRule(char modeChar, ModeRule modeRule)
    {
        modeRules[modeChar] = modeRule;
    }

    public static void Breakdown(IUser source, ChatObject target, string modeString,
        Queue<string> modeParameters)
    {
        var modeOperations = source.GetModeOperations();
        var modeFlag = true;

        foreach (var c in modeString)
            switch (c)
            {
                case '+':
                case '-':
                {
                    modeFlag = c == '+';
                    break;
                }
                default:
                {
                    var modeCollection = target.Modes;
                    var exists = modeCollection.HasMode(c);
                    var modeValue = exists ? modeCollection.GetModeChar(c) : -1;

                    var modeRule = modeCollection.GetMode(c);
                    if (modeRule == null)
                    {
                        // Unknown mode char
                        // :sky-8a15b323126 472 Sky S :is unknown mode char to me
                        source.Send(Raw.IRCX_ERR_UNKNOWNMODE_472(source.Server, source, c));
                        continue;
                    }

                    string parameter = string.Empty;
                    if (modeRule.RequiresParameter)
                    {
                        if (modeParameters != null && modeParameters.Count > 0)
                        {
                            parameter = modeParameters.Dequeue();
                        }
                        else
                        {
                            // Not enough parameters
                            //:sky-8a15b323126 461 Sky MODE +q :Not enough parameters
                            source.Send(Raw.IRCX_ERR_NEEDMOREPARAMS_461(source.Server, source,
                                $"{Resources.CommandMode} {c}"));
                            continue;
                        }
                    }


                    modeOperations.Enqueue(
                        new ModeOperation(
                            mode: modeRule,
                            source: source,
                            target: target,
                            modeFlag: modeFlag,
                            modeParameter: parameter
                        )
                    );

                    break;
                }
            }
    }
}