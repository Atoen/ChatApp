namespace Server.Exceptions;

public class CommandException : Exception
{
    public CommandException(string message, Exception? innerException = null) : base(message, innerException)
    {

    }
}