namespace Server.Commands;

public static class TypeExtensions
{
    private static readonly HashSet<Type> AssignableToIConvertible = new();

    public static bool ImplementsIConvertible(this Type type)
    {
        if (AssignableToIConvertible.Contains(type)) return true;
        
        if (!typeof(IConvertible).IsAssignableFrom(type)) return false;

        AssignableToIConvertible.Add(type);
        return true;
    }
}