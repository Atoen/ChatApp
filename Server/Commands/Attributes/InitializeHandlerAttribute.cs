using System.Reflection;
using MethodDecorator.Fody.Interfaces;

namespace Server.Commands.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class InitializeHandlerAttribute : Attribute, IMethodDecorator
{
    public void Init(object instance, MethodBase method, object[] args)
    {
        if (instance is not ICommandHandler commandHandler) return;

        var methodInfos = instance.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
        var commandHandlers = from methodInfo in methodInfos
            let attribute = methodInfo.GetCustomAttribute<CommandAttribute>()
            where attribute is not null
            select new {methodInfo, attribute.CommandName};

        foreach (var handler in commandHandlers)
        {
            var name = handler.CommandName;
            if (!commandHandler.CaseSensitive) name = name.ToLower();

            var (count, usesRemainder) = MatchCommandArgsCount(handler.methodInfo);

            var executor = CreateExecutor(count, usesRemainder, commandHandler, handler.methodInfo);

            commandHandler.Handlers.Add(name, executor);
        }
    }
    
    private static (int paramsCount, bool usesRemainder) MatchCommandArgsCount(MethodInfo methodInfo)
    {
        var parameterInfos = methodInfo.GetParameters();

        if (parameterInfos[0].ParameterType != typeof(User)) throw new TargetException();

        var hasRemainderAttribute = false;
        var remainderPosition = 0;
        for (var i = 1; i < parameterInfos.Length; i++)
        {
            if (hasRemainderAttribute) throw new TargetException("Remainder attribute must be placed on the last parameter");

            var parameterInfo = parameterInfos[i];
            var attribute = parameterInfo.GetCustomAttribute<RemainderAttribute>();
            if (attribute is not null)
            {
                hasRemainderAttribute = true;
                remainderPosition = i;
            }
        }

        return hasRemainderAttribute ? (remainderPosition, true) : (parameterInfos.Length - 1, false);
    }

    private static CommandExecutionContext CreateExecutor(int argsCount, bool useRemainder, ICommandHandler instance, MethodInfo methodInfo)
    {
        if (argsCount == 0)
        {
            var d0 = (CommandArgs0) Delegate.CreateDelegate(typeof(CommandArgs0), instance, methodInfo);
            return async (user, _) => await d0.Invoke(user);
        }

        switch (argsCount)
        {
            case 1:
                var d1 = (CommandArgs1) Delegate.CreateDelegate(typeof(CommandArgs1), instance, methodInfo);
                return useRemainder
                    ? async (user, args) => await d1.Invoke(user, string.Join(" ", args))
                    : async (user, args) => await d1.Invoke(user, args[0]);

            case 2:
                var d2 = (CommandArgs2) Delegate.CreateDelegate(typeof(CommandArgs2), instance, methodInfo);
                return useRemainder
                    ? async (user, args) => await d2.Invoke(user, args[0], string.Join(" ", args[1..]))
                    : async (user, args) => await d2.Invoke(user, args[0], args[1]);

            case 3:
                var d3 = (CommandArgs3) Delegate.CreateDelegate(typeof(CommandArgs3), instance, methodInfo);
                return useRemainder
                    ? async (user, args) => await d3.Invoke(user, args[0], args[1],string.Join(" ", args[2..]))
                    : async (user, args) => await d3.Invoke(user, args[0], args[1], args[2]);

            default:
                var d4 = (CommandArgs4) Delegate.CreateDelegate(typeof(CommandArgs4), instance, methodInfo);
                return useRemainder
                    ? async (user, args) => await d4.Invoke(user, args[0], args[1], args[2], string.Join(" ", args[3..]))
                    : async (user, args) => await d4.Invoke(user, args[0], args[1], args[2], args[2..]);
        }
    }

    private delegate Task CommandArgs0(User user);
    private delegate Task CommandArgs1(User user, string arg1);
    private delegate Task CommandArgs2(User user, string arg1, string arg2);
    private delegate Task CommandArgs3(User user, string arg1, string arg2, string arg3);
    private delegate Task CommandArgs4(User user, string arg1, string arg2, string arg3, params string[] rest);

    public void OnEntry() { }

    public void OnExit() { }

    public void OnException(Exception exception) { }
}