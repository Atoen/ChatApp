namespace Server.Exceptions;

public class ModuleException : Exception
{
    public ModuleException(string message, Exception? innerException = null) : base(message, innerException)
    {

    }
}