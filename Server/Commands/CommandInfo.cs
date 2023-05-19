using System.Reflection;
using Server.Attributes;

namespace Server.Commands;

public class CommandInfo
{
    public ModuleInfo Module { get; internal set; } = null!;
    public string Name { get; internal set; } = "";
    public string Summary { get; internal set; } = "";

    public ExtraArgsHandleMode ExtraArgsHandleMode { get; internal set; } = ExtraArgsHandleMode.Ignore;
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
    
    public static CommandInfo CreateCommandInfo(MethodInfo method, ModuleInfo module)
    {
        var commandInfo = new CommandInfo(method)
        {
            Module = module
        };

        var attributes = method.GetCustomAttributes();
        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case CommandAttribute command:
                    if (commandInfo.Name == string.Empty) commandInfo.Name = command.Text;
                    break;

                case NameAttribute name:
                    commandInfo.Name = name.Text;
                    break;

                case SummaryAttribute summary:
                    commandInfo.Summary = summary.Text;
                    break;

                case AliasAttribute alias:
                    commandInfo.WithAlias(alias.Aliases);
                    break;

                case ExtraArgsAttribute extraArgs:
                    commandInfo.ExtraArgsHandleMode = extraArgs.HandleMode;
                    break;

                default:
                    commandInfo.AddAttribute(attribute);
                    break;
            }
        }

        var parameters = method.GetParameters()
            .Where(x => x.ParameterType != typeof(CommandContext))
            .Select(x => ParameterInfo.CreateParameterInfo(x, commandInfo));
        
        commandInfo.AddParameters(parameters);

        return commandInfo;
    }
}