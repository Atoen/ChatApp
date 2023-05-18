namespace Server.Commands;

public abstract class Module
{
    protected CommandContext Context;

    public void SetContext(CommandContext context) => Context = context;
}