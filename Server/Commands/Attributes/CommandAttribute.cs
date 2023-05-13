namespace Server.Commands.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public CommandAttribute(string commandName) => CommandName = commandName;

    public string CommandName { get; }
}