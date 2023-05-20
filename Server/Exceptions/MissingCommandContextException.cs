namespace Server.Exceptions;

public class MissingCommandContextException : CommandException
{
    public MissingCommandContextException(string message, Exception? innerException = null) : base(message, innerException)
    {

    }
}