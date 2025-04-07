using Irc.Enumerations;

namespace Irc.Interfaces;

public interface ICommand
{
    EnumCommandDataType GetDataType();
    string GetName();
    void Execute(IChatFrame chatFrame);
    bool ParametersAreValid(IChatFrame chatFrame);
    bool RegistrationNeeded(IChatFrame chatFrame);
}