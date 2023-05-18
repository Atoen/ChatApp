namespace Server.Commands;

public class ModuleInfo
{
    public string Name { get; internal set; } = "";
    public string Summary { get; internal set; } = "";
    public IReadOnlyList<CommandInfo> Commands => _commands;
    public IReadOnlyList<Attribute> Attributes => _attributes;
    public ModuleInfo? Parent { get; internal set; }
    public bool IsSubmodule => Parent is not null;

    public ModuleInfo(Module module) => Module = module;

    internal Module Module { get; }
    
    private readonly List<Attribute> _attributes = new();
    private readonly List<CommandInfo> _commands = new();
    
    public void AddAttribute(Attribute attribute) => _attributes.Add(attribute);
    public void AddCommands(IEnumerable<CommandInfo> commands) => _commands.AddRange(commands);
}