namespace Server.Attributes;

[AttributeUsage(AttributeTargets.Method)]

public class ExtraArgsAttribute : Attribute
{
    public ExtraArgsAttribute(ExtraArgsHandleMode handleMode = ExtraArgsHandleMode.Ignore) => HandleMode = handleMode;

    public ExtraArgsHandleMode HandleMode { get; }
}


public enum ExtraArgsHandleMode
{
    Ignore,
    Throw
}