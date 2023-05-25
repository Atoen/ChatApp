using Server.Commands;

namespace Server;

public static class TypeExtensions
{
    private static readonly HashSet<Type> AssignableToIConvertible = new();
    private static readonly HashSet<Type> AssignableToISpanParsable = new();

    public static bool IsConvertibleOrParsable(this Type type, out ReadMode readMode)
    {
        readMode = ReadMode.Convert;
        if (ImplementsIConvertible(type)) return true;

        if (ImplementsISpanParsable(type))
        {
            readMode = ReadMode.Parse;
            return true;
        }

        return false;
    }

    public static bool ImplementsIConvertible(this Type type)
    {
        if (AssignableToIConvertible.Contains(type)) return true;

        if (!typeof(IConvertible).IsAssignableFrom(type)) return false;

        AssignableToIConvertible.Add(type);
        return true;
    }

    public static bool ImplementsISpanParsable(this Type type)
    {
        if (AssignableToISpanParsable.Contains(type)) return true;

        var implements = type.GetInterfaces()
            .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ISpanParsable<>));

        if (!implements) return false;

        AssignableToISpanParsable.Add(type);
        return true;
    }
}