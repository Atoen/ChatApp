namespace Server.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Parameter)]
public class AliasAttribute : Attribute
{
    public AliasAttribute(params string[] aliases) => Aliases = aliases;

    public string[] Aliases { get; }
}