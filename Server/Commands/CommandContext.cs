namespace Server.Commands;

public struct CommandContext
{
    public CommandContext(User user, params string[] args)
    {
        User = user;
        Args = args;
    }
    
    public User User { get; }
    public string[] Args { get; }

    public async Task Respond(string message) => await User.Respond(message);
}