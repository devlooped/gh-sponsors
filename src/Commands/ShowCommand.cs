using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

namespace Devlooped.SponsorLink;

public class ShowCommand : Command
{
    public override int Execute(CommandContext context)
    {
        if (!GitHub.IsInstalled)
        {
            AnsiConsole.MarkupLine("[yellow]Please install GitHub CLI from [/][link]https://cli.github.com/[/]");
            return -1;
        }

        if (GitHub.Authenticate() is not { } account)
        {
            AnsiConsole.MarkupLine("Please run [yellow]gh auth login[/] to authenticate, [yellow]gh auth status -h github.com[/] to verify your status.");
            return -1;
        }

        var json = JsonSerializer.Serialize(account, JsonOptions.Default);
        AnsiConsole.Write(
            new Panel(new JsonText(json))
                .Header("Authenticated")
                .Collapse()
                .RoundedBorder()
                .BorderColor(Color.Green));

        //AnsiConsole.MarkupLine($"[green]Authenticated as {account.Login}[/] [grey](id={account.Id})[/]");

        return 0;
    }
}
