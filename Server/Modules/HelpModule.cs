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

        foreach (var command in _commandService.Commands.DistinctBy(x => x.Name))
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

        await context.NotifyAsync(builder.ToString()).ConfigureAwait(false);
    }

    [Command("help"), Alias("h"), Summary("Displays information about specified command")]
    public async Task HelpCommand(CommandContext context, string commandName)
    {
        var commandMatches = _commandService.Commands.Where(x => x.Name == commandName).ToList();
        if (commandMatches.Count == 0)
        {
            await context.NotifyAsync($"Couldn't find command {commandName}.");
            return;
        }

        var builder = new StringBuilder();
        foreach (var command in commandMatches)
        {
            builder.Append(command.Name);

            if (command.Aliases.Count > 0)
            {
                builder.Append($" [{string.Join(", ", command.Aliases)}]");
            }

            if (!string.IsNullOrEmpty(command.Summary))
            {
                builder.Append(" - ");
                builder.Append(command.Summary);
            }

            if (command.Parameters.Count == 0)
            {
                builder.Append(" (no params) ");
            }
            else
            {
                builder.Append(" params: \n");
                foreach (var parameter in command.Parameters)
                {
                    builder.Append(' ');

                    if (parameter.IsRemainder)
                    {
                        builder.Append("[Remainder]");
                    }

                    builder.Append($" {parameter.Type.Name} - ");
                    builder.Append(string.IsNullOrEmpty(parameter.Summary) ? parameter.Name : parameter.Summary);

                    if (parameter.IsOptional)
                    {
                        builder.Append($" = {parameter.DefaultValue}");
                    }

                    builder.Append('\n');
                }
            }

            builder.Append('\n');
        }

        await context.NotifyAsync(builder.ToString()).ConfigureAwait(false);
    }
}
