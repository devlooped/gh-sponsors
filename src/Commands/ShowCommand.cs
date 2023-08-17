using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

namespace Devlooped.SponsorLink;

public class ShowCommand : GitHubCommand
{
    protected override int OnExecute(Account account, CommandContext context)
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
