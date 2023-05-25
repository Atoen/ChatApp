using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Server.Commands;

public class TypeReader
{
    public TypeReader(CultureInfo culture) => Culture = culture;

    public CultureInfo Culture { get; }

    private static readonly ConcurrentDictionary<Type, MethodInfo> ParseMethodCache = new();

    public object Read(string input, ParameterInfo parameter)
    {
        if (parameter.ReadMode == ReadMode.Convert)
        {
            return Convert.ChangeType(input, parameter.Type, Culture);
        }

        var parseMethod = GetParseMethod(parameter.Type);
        var result = parseMethod.Invoke(null, new object?[] {input, CultureInfo.InvariantCulture})!;

        return result;
    }
    
    private static MethodInfo GetParseMethod(Type type)
    {
        if (!ParseMethodCache.TryGetValue(type, out var parseMethod))
        {
            parseMethod = type.GetMethod("Parse", new[] {typeof(string), typeof(IFormatProvider)})!;
            ParseMethodCache[type] = parseMethod;
        }

        return parseMethod;
    }
}

public enum ReadMode
{
    Convert,
    Parse
}
