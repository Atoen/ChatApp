namespace Server.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter)]
public class NameAttribute : Attribute
{
    public NameAttribute(string text) => Text = text;

    public string Text { get; }
}