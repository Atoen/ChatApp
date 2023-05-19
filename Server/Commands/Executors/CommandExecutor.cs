using Server.Attributes;

namespace Server.Commands.Executors;

public abstract class CommandExecutor
{
    protected readonly CommandInfo CommandInfo;

    protected CommandExecutor(CommandInfo command) => CommandInfo = command;

    public virtual async Task Execute(CommandContext context)
    {
        if (context.Args.Length > 0 && CommandInfo.ExtraArgsHandleMode == ExtraArgsHandleMode.Throw)
        {
            throw new InvalidOperationException("Command was invoked with too many parameters.");
        }

        await Invoke(context, Array.Empty<object>());
    }

    protected abstract Task Invoke(CommandContext context, object[] args);
}