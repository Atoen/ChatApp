using Server.Attributes;

namespace Server.Commands.Executors;

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

        await Invoke(context, context.Args);
    }

    protected abstract Task Invoke(CommandContext context, string[] args);
}