namespace Server.Commands;

public class MissingCommandContextException : CommandException
{
    public MissingCommandContextException(string message) : base(message)
    {
        
    }
}