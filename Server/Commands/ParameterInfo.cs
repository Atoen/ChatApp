using System.Collections.Immutable;
using System.Reflection;
using Server.Attributes;

namespace Server.Commands;

public class ParameterInfo
{
    public CommandInfo Command { get; internal set; } = null!;
    public Type Type { get; internal set; } = null!;
    public object? DefaultValue { get; internal set; }

    public string Name { get; internal set; } = "";
    public string Summary { get; internal set; } = "";

    public bool IsOptional { get; internal set; }
    public bool IsRemainder { get; internal set; }

    public IReadOnlyList<Attribute> Attributes => _attributes;
    public IReadOnlyList<string> Aliases => _aliases;

    private readonly List<string> _aliases = new();
    private readonly List<Attribute> _attributes = new();

    public void WithAlias(params string[] aliases) => _aliases.AddRange(aliases);
    public void AddAttribute(Attribute attribute) => _attributes.Add(attribute);

    public override string ToString() => Name;
    
    public static ParameterInfo CreateParameterInfo(System.Reflection.ParameterInfo parameter, CommandInfo command)
    {
        var parameterType = parameter.ParameterType;
        if (parameterType != typeof(string) &&
            !parameterType.ImplementsIConvertible())
        {
            throw new ArgumentException("Command parameter type must be string or implement IConvertible interface",
                parameter.Name);
        }
        
        var parameterInfo = new ParameterInfo
        {
            Type = parameterType,
            Command = command,
            IsOptional = parameter.IsOptional,
            DefaultValue = parameter.DefaultValue,
            Name = parameter.Name ?? string.Empty
        };

        var attributes = parameter.GetCustomAttributes().ToImmutableArray();
        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case NameAttribute name:
                    parameterInfo.Name = name.Text;
                    break;

                case AliasAttribute alias:
                    parameterInfo.WithAlias(alias.Aliases);
                    break;

                case SummaryAttribute summary:
                    parameterInfo.Summary = summary.Text;
                    break;

                case RemainderAttribute:
                    if (parameterInfo.Type != typeof(string))
                    {
                        throw new InvalidAttributeUsageException("RemainderAttribute can only be used on string parameters.");
                    }

                    parameterInfo.IsRemainder = true;
                    break;

                default:
                    parameterInfo.AddAttribute(attribute);
                    break;
            }
        }

        return parameterInfo;
    }
}