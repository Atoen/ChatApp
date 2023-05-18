namespace Server.Attributes;

public class InvalidAttributeUsageException : Exception
{
    public InvalidAttributeUsageException(string message) : base(message)
    {
        
    }
}