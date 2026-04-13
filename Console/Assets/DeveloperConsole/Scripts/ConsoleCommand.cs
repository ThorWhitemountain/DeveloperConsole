public class ConsoleCommand : System.Attribute
{
    // command name
    private string commandName;

    // Optional parameter: command default value (as string)
    private string value;

    // Optional parameter: Command info
    private string info;

    // Optional parameter: if set to true, command will only be registered in debug builds (Editor and Development build)
    private bool debugOnlyCommand;

    // Optional parameter: if set to true, command won't show up in predictions when using Minimal GUI
    private bool hiddenCommandMinimalGUI;

    // Optional parameter: if set to true, command won't show up in predictions
    private bool fullyHiddenCommand;

    public ConsoleCommand(string commandName, string value = "", string info = "",
        bool debugOnlyCommand = false, bool hiddenCommandMinimalGUI = false, bool hiddenCommand = false)
    {
        this.commandName = commandName;
        this.value = value;
        this.info = info;
        this.debugOnlyCommand = debugOnlyCommand;
        this.hiddenCommandMinimalGUI = hiddenCommandMinimalGUI;
        fullyHiddenCommand = hiddenCommand;
    }

    public string GetCommandName()
    {
        return commandName;
    }

    public string GetValue()
    {
        return value;
    }

    public string GetInfo()
    {
        return info;
    }

    public bool IsDebugOnlyCommand()
    {
        return debugOnlyCommand;
    }

    public bool IsHiddenCommand()
    {
        return fullyHiddenCommand;
    }

    public bool IsHiddenMinimalGUI()
    {
        return hiddenCommandMinimalGUI;
    }
}