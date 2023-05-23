namespace Server.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class OverloadPriorityAttribute : Attribute
{
    public OverloadPriorityAttribute(int value) => Value = value;

    public int Value { get; }
}