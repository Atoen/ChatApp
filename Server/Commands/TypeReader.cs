using System.Globalization;

namespace Server.Commands;

public class TypeReader
{
    public TypeReader(CultureInfo culture) => Culture = culture;

    public CultureInfo Culture { get; }

    public object Read(string input, Type type) => Convert.ChangeType(input, type, Culture);
}
