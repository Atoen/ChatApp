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
}