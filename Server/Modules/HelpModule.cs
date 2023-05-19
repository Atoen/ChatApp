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
        foreach (var command in _commandService.Commands)
        {
            await context.Respond($"{command.Name} ({string.Join(", ", command.Aliases)}) {command.Summary}");
        }
    }
}
