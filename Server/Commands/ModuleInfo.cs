using System.Reflection;
using Serilog;
using Server.Attributes;

namespace Server.Commands;

public class ModuleInfo
{
    public string Name { get; internal set; } = "";
    public string Summary { get; internal set; } = "";
    public IReadOnlyList<CommandInfo> Commands => _commands;
    public IReadOnlyList<Attribute> Attributes => _attributes;
    public ModuleInfo? Parent { get; internal set; }
    public bool IsSubmodule => Parent is not null;

    public ModuleInfo(Module instance) => Instance = instance;

    internal Module Instance { get; }

    private readonly List<Attribute> _attributes = new();
    private readonly List<CommandInfo> _commands = new();

    public void AddAttribute(Attribute attribute) => _attributes.Add(attribute);
    public void AddCommands(IEnumerable<CommandInfo> commands) => _commands.AddRange(commands);
    
    public static ModuleInfo CreateModuleInfo(Module module)
    {
        var moduleInfo = new ModuleInfo(module);

        var attributes = module.GetType().GetCustomAttributes();
        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case NameAttribute name:
                    moduleInfo.Name = name.Text;
                    break;

                case SummaryAttribute summary:
                    moduleInfo.Summary = summary.Text;
                    break;

                default:
                    moduleInfo.AddAttribute(attribute);
                    break;
            }
        }

        if (moduleInfo.Name == string.Empty)
        {
            moduleInfo.Name = module.GetType().Name;
        }

        var methods = from methodInfo in module.GetType().GetMethods()
            let commandAttribute = methodInfo.GetCustomAttribute<CommandAttribute>()
            where commandAttribute is not null
            select methodInfo;

        var commands = methods.Select(x => CommandInfo.CreateCommandInfo(x, moduleInfo));
        moduleInfo.AddCommands(commands);

        Log.Debug("Loaded {ModuleName} module", moduleInfo.Name);

        return moduleInfo;
    }
}