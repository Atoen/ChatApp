namespace Server.Commands;

public interface ICommandExecutor
{
    Task Execute(CommandContext context);
}