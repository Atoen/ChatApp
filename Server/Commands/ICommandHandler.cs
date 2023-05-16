namespace Server.Commands;

public interface ICommandHandler
{
    char Prefix { get; }

    public bool CaseSensitive { get; set; }

    Task Handle(User sender, string command);
}

public delegate Task CommandExecutionContext(User user, params string[] args);