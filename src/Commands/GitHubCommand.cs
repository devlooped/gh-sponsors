using System.Security.Principal;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Devlooped.SponsorLink;

public abstract class GitHubCommand : Command
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

        return OnExecute(account, context);
    }

    abstract protected int OnExecute(Account account, CommandContext context);
}
