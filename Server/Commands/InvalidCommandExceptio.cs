namespace Server.Commands;

public class CommandException : Exception
{
    public CommandException(string message) : base(message)
    {
        
    }
}