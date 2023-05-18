using System.Reflection;

namespace Server.Commands;

public class CommandInfo
{
    public ModuleInfo Module { get; internal set; } = null!;
    public string Name { get; internal set; } = "";
    public string Summary { get; internal set; } = "";
    public IReadOnlyList<string> Aliases => _aliases;
    public IReadOnlyList<ParameterInfo> Parameters => _parameters;
    public IReadOnlyList<Attribute> Attributes => _attributes;
    public IEnumerable<string> InvokeNames => _aliases.Concat(new[] {Name});
    
    public CommandInfo(MethodInfo methodBase) => Method = methodBase;

    internal MethodInfo Method { get; }

    private readonly List<string> _aliases = new();
    private readonly List<Attribute> _attributes = new();
    private readonly List<ParameterInfo> _parameters = new();
    
    public void WithAlias(params string[] aliases) => _aliases.AddRange(aliases);
    public void AddAttribute(Attribute attribute) => _attributes.Add(attribute);
    public void AddParameters(IEnumerable<ParameterInfo> parameters) => _parameters.AddRange(parameters);
}