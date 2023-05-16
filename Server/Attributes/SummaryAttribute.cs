namespace Server.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter)]
public class SummaryAttribute : Attribute
{
    public SummaryAttribute(string text) => Text = text;

    public string Text { get; }
}