namespace Server.Commands.Executors;

public abstract class CommandParamExecutor : CommandExecutor
{
    protected readonly TypeReader TypeReader;
    protected readonly bool UseRemainder;
    
    protected CommandParamExecutor(CommandInfo command, TypeReader typeReader) : base(command)
    {
        TypeReader = typeReader;
        UseRemainder = command.Parameters[^1].IsRemainder;
    }
}