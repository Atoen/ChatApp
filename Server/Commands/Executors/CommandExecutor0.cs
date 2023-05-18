namespace Server.Commands.Executors;

public class CommandExecutor0 : CommandExecutor
{
    private delegate Task Executor();

    private readonly Executor _executor;

    public CommandExecutor0(CommandInfo command) : base(command)
    {
        _executor = (Executor) Delegate.CreateDelegate(typeof(Executor), command.Module.Instance, command.Method);
    }

    protected override async Task Invoke(string[] args) => await _executor.Invoke();
}