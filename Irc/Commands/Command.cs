using Irc.Constants;
using Irc.Enumerations;
using Irc.Interfaces;

namespace Irc.Commands;

public class Command : ICommand
{
    private readonly bool _registrationRequired;
    protected int _requiredMaximumParameters;
    protected int _requiredMinimumParameters;

    public Command(int requiredMinimumParameters = 0, bool registrationRequired = true,
        int requiredMaximumParameters = -1)
    {
        _requiredMinimumParameters = requiredMinimumParameters;
        _registrationRequired = registrationRequired;
        _requiredMaximumParameters = requiredMaximumParameters;
    }

    public string GetName()
    {
        return GetType().Name;
    }

    public EnumCommandDataType GetDataType()
    {
        throw new NotImplementedException();
    }

    public void Execute(IChatFrame chatFrame)
    {
        throw new NotImplementedException();
    }

    public bool ParametersAreValid(IChatFrame chatFrame)
    {
        var parameterCount = chatFrame.Message.Parameters.Count;

        if (parameterCount < _requiredMinimumParameters)
        {
            chatFrame.User.Send(Raws.IRCX_ERR_NEEDMOREPARAMS_461(chatFrame.Server, chatFrame.User, GetName()));
            return false;
        }

        if (_requiredMaximumParameters > 0 && parameterCount > _requiredMaximumParameters)
        {
            chatFrame.User.Send(Raws.IRCX_ERR_TOOMANYARGUMENTS_901(chatFrame.Server, chatFrame.User, GetName()));
            return false;
        }

        return true;
    }

    public bool RegistrationNeeded(IChatFrame chatFrame)
    {
        if (!_registrationRequired || (_registrationRequired && chatFrame.User.IsRegistered())) return false;

        chatFrame.User.Send(Raws.IRCX_ERR_NOTREGISTERED_451(chatFrame.Server, chatFrame.User));
        return true;
    }
}