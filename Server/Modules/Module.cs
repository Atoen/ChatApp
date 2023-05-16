namespace Server.Modules;

public abstract class Module
{

}

public class ModuleInfo
{
    public string Name { get; internal set; } = "";
    public string Summary { get; internal set; } = "";
    public IReadOnlyList<CommandInfo> Commands => _commands;
    public IReadOnlyList<Attribute> Attributes => _attributes;
    public ModuleInfo? Parent { get; internal set; }
    public bool IsSubmodule => Parent is not null;
    
    private readonly List<Attribute> _attributes = new();
    private readonly List<CommandInfo> _commands = new();
    
    public void AddAttribute(Attribute attribute) => _attributes.Add(attribute);
    public void AddCommands(IEnumerable<CommandInfo> commands) => _commands.AddRange(commands);
}

public class CommandInfo
{
    public ModuleInfo Module { get; internal set; } = null!;
    public string Name { get; internal set; } = "";
    public string Summary { get; internal set; } = "";
    public IReadOnlyList<string> Aliases => _aliases;
    public IReadOnlyList<ParameterInfo> Parameters => _parameters;
    public IReadOnlyList<Attribute> Attributes => _attributes;
    
    private readonly List<string> _aliases = new();
    private readonly List<Attribute> _attributes = new();
    private readonly List<ParameterInfo> _parameters = new();
    
    public void WithAlias(params string[] aliases) => _aliases.AddRange(aliases);

    public void AddAttribute(Attribute attribute) => _attributes.Add(attribute);
    public void AddParameters(IEnumerable<ParameterInfo> parameters) => _parameters.AddRange(parameters);
}

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
