using System.Reflection;
using Server.Attributes;
using Server.Exceptions;

namespace Server.Commands;

public class CommandInfo
{
    public ModuleInfo Module { get; private init; } = null!;
    public string Name { get; private set; } = "";
    public string Summary { get; private set; } = "";
    public int Priority { get; private set; }

    public ExtraArgsHandleMode ExtraArgsHandleMode { get; private set; } = ExtraArgsHandleMode.Ignore;
    public IReadOnlyList<string> Aliases => _aliases;
    public IReadOnlyList<ParameterInfo> Parameters => _parameters;
    public IReadOnlyList<Attribute> Attributes => _attributes;
    public IEnumerable<string> InvokeNames => _aliases.Concat(new[] {Name});

    public CommandInfo(MethodInfo methodBase) => Method = methodBase;

    internal MethodInfo Method { get; }

    private readonly List<string> _aliases = new();
    private readonly List<Attribute> _attributes = new();
    private readonly List<ParameterInfo> _parameters = new();

    private void WithAlias(params string[] aliases) => _aliases.AddRange(aliases);
    private void AddAttribute(Attribute attribute) => _attributes.Add(attribute);
    private void AddParameters(IEnumerable<ParameterInfo> parameters) => _parameters.AddRange(parameters);

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
                
                case OverloadPriorityAttribute priority:
                    commandInfo.Priority = priority.Value;
                    break;

                default:
                    commandInfo.AddAttribute(attribute);
                    break;
            }
        }

        ValidateCommand(method, commandInfo);

        var parameters = method.GetParameters();

        commandInfo.AddParameters(parameters.Skip(1)
                .Select(x => ParameterInfo.CreateParameterInfo(x, commandInfo)));

        return commandInfo;
    }

    private static void ValidateCommand(MethodInfo method, CommandInfo commandInfo)
    {
        if (method.IsStatic)
        {
            throw new CommandException($"Command {commandInfo} cannot be static.");
        }

        var parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            throw new MissingCommandContextException(
                $"Command {commandInfo} is missing the required {typeof(CommandContext)} parameter.");
        }

        if (parameters[0].ParameterType != typeof(CommandContext))
        {
            throw new MissingCommandContextException(
                $"Command {commandInfo} first parameter must be of type {typeof(CommandContext)}.");
        }

        if (method.ReturnType != typeof(Task))
        {
            throw new CommandException($"Command {commandInfo} return type must be {typeof(Task)}");
        }
    }

    public override string ToString() => $"{Name} ({Module.Name})";
}