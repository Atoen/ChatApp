namespace Server.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public CommandAttribute(string text) => Text = text;

    public string Text { get; }
}