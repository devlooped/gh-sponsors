using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

namespace Devlooped.SponsorLink;

public class ShowCommand(Account account) : Command
{
    public override int Execute(CommandContext context)
    {
        var json = JsonSerializer.Serialize(account, JsonOptions.Default);
        AnsiConsole.Write(
            new Panel(new JsonText(json))
                .Header("Authenticated")
                .Collapse()
                .RoundedBorder()
                .BorderColor(Color.Green));

        return 0;
    }
}
