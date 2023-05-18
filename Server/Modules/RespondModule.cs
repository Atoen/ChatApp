using System.Globalization;
using Server.Attributes;
using Server.Commands;

namespace Server.Modules;

[Name("Basic Commands")]
public class RespondModule : Module
{
    [Command("ping"), Alias("p"), Summary("Pongs the ping")]
    public async Task Ping() => await Context.Respond("Pong!");

    [Command("me")]
    public async Task MeCommand([Remainder] string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            await Context.User.Respond(text);
        }
    }

    [Command("time"), Alias("t"), Summary("Displays current server time")]
    public async Task TimeCommand() => await Context.Respond(DateTime.Now.ToLongTimeString());

    [Command("calculate"), Alias("calc", "c"), ExtraArgs(ExtraArgsHandleMode.Throw)]
    public async Task NumberCommand(double num1, char @operator, double num2)
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

        await Context.Respond(result.ToString(CultureInfo.InvariantCulture));
    }
}
