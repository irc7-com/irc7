using Irc.Constants;
using Irc.Interfaces;
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

    public static void Breakdown(IUser source, IChatObject target, string modeString,
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
                    var modeValue = exists ? modeCollection.GetModeValue(c) : -1;

                    if (!modeCollection.HasMode(c))
                    {
                        // Unknown mode char
                        // :sky-8a15b323126 472 Sky S :is unknown mode char to me
                        source.Send(Raws.IRCX_ERR_UNKNOWNMODE_472(source.Server, source, c));
                        continue;
                    }

                    var modeRule = modeCollection[c];
                    var parameter = string.Empty;
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
                            source.Send(Raws.IRCX_ERR_NEEDMOREPARAMS_461(source.Server, source,
                                $"{Resources.CommandMode} {c}"));
                            continue;
                        }
                    }


                    modeOperations.Enqueue(
                        new ModeOperation(
                            modeRule,
                            source,
                            target,
                            modeFlag,
                            parameter
                        )
                    );

                    break;
                }
            }
    }
}