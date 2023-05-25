using System.Globalization;
using Server.Attributes;
using Server.Commands;

namespace Server.Modules;

[Name("Basic Commands")]
public class RespondModule : Module
{
    [Command("ping"), Alias("p"), Summary("Pongs the ping")]
    public async Task Ping(CommandContext context) => await context.RespondAsync("Pong!").ConfigureAwait(false);

    [Command("me")]
    public async Task MeCommand(CommandContext context, [Remainder] string text)
    {
        await context.BroadcastAsync(text).ConfigureAwait(false);
    }

    [Command("time"), Alias("t"), Summary("Displays current server time")]
    public async Task TimeCommand(CommandContext context) => await context.RespondAsync(DateTime.Now.ToLongTimeString()).ConfigureAwait(false);

    [Command("calculate"), Alias("calc", "c"), ExtraArgs(ExtraArgsHandleMode.Throw)]
    public async Task CalculateCommand(CommandContext context, double num1, char @operator, double num2)
    {
        var result = @operator switch
        {
            '+' => num1 + num2,
            '-' => num1 - num2,
            '*' => num1 * num2,
            '/' => num1 / num2,
            '^' => Math.Pow(num1, num2),
            _ => throw new ArgumentOutOfRangeException(nameof(@operator))
        };

        await context.RespondAsync(result.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
    }
}
