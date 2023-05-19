using System.Reflection;
using Serilog;
using Server.Attributes;

namespace Server.Commands;

public class ModuleInfo
{
    public string Name { get; private set; } = "";
    public string Summary { get; private set; } = "";
    public IReadOnlyList<CommandInfo> Commands => _commands;
    public IReadOnlyList<Attribute> Attributes => _attributes;
    public ModuleInfo? Parent { get; private set; }
    public bool IsSubmodule => Parent is not null;

    public ModuleInfo(Module instance) => Instance = instance;

    internal Module Instance { get; }

    private readonly List<Attribute> _attributes = new();
    private readonly List<CommandInfo> _commands = new();

    private void AddAttribute(Attribute attribute) => _attributes.Add(attribute);
    private void AddCommands(IEnumerable<CommandInfo> commands) => _commands.AddRange(commands);

    public static ModuleInfo CreateModuleInfo(Module module)
    {
        var moduleType = module.GetType();
        var moduleInfo = new ModuleInfo(module)
        {
            Name = moduleType.Name
        };

        var attributes = moduleType.GetCustomAttributes();
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

        var methods = from methodInfo in moduleType.GetMethods()
            let commandAttribute = methodInfo.GetCustomAttribute<CommandAttribute>()
            where commandAttribute is not null
            select methodInfo;

        var commands = methods.Select(x => CommandInfo.CreateCommandInfo(x, moduleInfo));
        moduleInfo.AddCommands(commands);

        Log.Debug("Loaded {ModuleName} module", moduleInfo.Name);

        return moduleInfo;
    }
}