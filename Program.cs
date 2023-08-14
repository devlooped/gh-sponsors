using Devlooped.SponsorLink;
using Spectre.Console;

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

AnsiConsole.MarkupLine($"[green]Authenticated as {account.Login}[/] [grey](id={account.Id})[/]");

return 0;
