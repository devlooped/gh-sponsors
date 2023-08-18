using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Devlooped.SponsorLink;

public abstract class GitHubCommand<TSettings> : Command<TSettings>
    where TSettings : CommandSettings
{
    public sealed override int Execute([NotNull] CommandContext context, [NotNull] TSettings settings)
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

        return OnExecute(account, settings, context);
    }

    abstract protected int OnExecute(Account account, TSettings settings, CommandContext context);
}

public abstract class GitHubCommand : Command
{
    public sealed override int Execute(CommandContext context)
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
