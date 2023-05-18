using Server.Attributes;

namespace Server.Commands;

public abstract class CommandExecutor
{
    private readonly CommandInfo _commandInfo;
    private readonly ExtraArgsHandleMode _extraArgsHandleMode;

    protected CommandExecutor(CommandInfo command)
    {
        _commandInfo = command;
        _extraArgsHandleMode = command.ExtraArgsHandleMode;
    }

    public async Task Execute(CommandContext context)
    {
        var commandParamCount = _commandInfo.Parameters.Count;
        var args = context.Args.Length;

        if (args < commandParamCount)
        {
            throw new InvalidOperationException("Command was invoked with too few parameters.");
        }

        if (args > commandParamCount && _extraArgsHandleMode == ExtraArgsHandleMode.Throw)
        {
            throw new InvalidOperationException("Command was invoked with too many parameters.");
        }

        var moduleInstance = _commandInfo.Module.Instance;
        await moduleInstance.SetContext(context);

        try
        {
            await Invoke(context.Args);
        }
        finally
        {
            moduleInstance.ReleaseContext();
        }
    }

    protected abstract Task Invoke(string[] args);
}