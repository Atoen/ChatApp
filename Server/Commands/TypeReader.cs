using System.Globalization;

namespace Server.Commands;

public class TypeReader
{
    public TypeReader(CultureInfo culture) => Culture = culture;

    public CultureInfo Culture { get; }

    public T Read<T>(string input) => (T) Convert.ChangeType(input, typeof(T), Culture);
}
