namespace Server.Commands;

public abstract class CommandExecutor
{
    protected readonly CommandInfo CommandInfo;

    protected CommandExecutor(CommandInfo command)
    {
        CommandInfo = command;
    }

    public async Task Execute(CommandContext context)
    {
        var moduleInstance = CommandInfo.Module.Instance;
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