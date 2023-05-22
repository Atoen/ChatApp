using System.Text;
using Server.Attributes;
using Server.Commands;

namespace Server.Modules;

[Name("Helpful commands")]
public class HelpModule : Module
{
    private readonly CommandService _commandService;

    public HelpModule(CommandService commandService) => _commandService = commandService;

    [Command("help"), Alias("h"), Summary("Displays all available commands")]
    public async Task HelpCommand(CommandContext context)
    {
        var builder = new StringBuilder();

        foreach (var command in _commandService.Commands)
        {
            builder.Append(command.Name);

            if (command.Aliases.Count > 0)
            {
                builder.Append($" ({string.Join(", ", command.Aliases)})");
            }

            if (command.Summary != string.Empty)
            {
                builder.Append($" - {command.Summary}");
            }

            builder.Append('\n');
        }

        await context.NotifyAsync(builder.ToString());
    }
}
