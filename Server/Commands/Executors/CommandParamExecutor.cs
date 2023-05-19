using Server.Attributes;

namespace Server.Commands.Executors;

public abstract class CommandParamExecutor : CommandExecutor
{
    private readonly TypeReader _typeReader;
    private readonly ExtraArgsHandleMode _extraArgsHandleMode;

    protected CommandParamExecutor(CommandInfo command, TypeReader typeReader) : base(command)
    {
        _typeReader = typeReader;
        _extraArgsHandleMode = command.ExtraArgsHandleMode;
    }

    public override async Task Execute(CommandContext context)
    {
        ValidateArgCount(context);

        await Invoke(context, ParseArgs(context.Args));
    }

    private void ValidateArgCount(CommandContext context)
    {
        var commandParamCount = CommandInfo.Parameters.Count;
        var argCount = context.Args.Length;

        if (argCount < commandParamCount)
        {
            var missingParams = commandParamCount - argCount;
            var optionalParamCount = CommandInfo.Parameters.Count(x => x.IsOptional);

            if (missingParams > optionalParamCount)
            {
                throw new InvalidOperationException("Command was invoked with too few parameters.");
            }
        }

        else if (argCount > commandParamCount && _extraArgsHandleMode == ExtraArgsHandleMode.Throw)
        {
            throw new InvalidOperationException("Command was invoked with too many parameters.");
        }
    }

    private object[] ParseArgs(string[] args)
    {
        var parametersCount = CommandInfo.Parameters.Count;
        var parsedArgs = new object[parametersCount];

        for (var i = 0; i < args.Length; i++)
        {
            if (i >= parametersCount) break;

            var parameter = CommandInfo.Parameters[i];
            var toRead = parameter.IsRemainder
                ? string.Join(' ', args[i..])
                : args[i];

            parsedArgs[i] = _typeReader.Read(toRead, parameter.Type);
        }

        for (var i = args.Length; i < parametersCount; i++)
        {
            parsedArgs[i] = CommandInfo.Parameters[i].DefaultValue!;
        }

        return parsedArgs;
    }
}