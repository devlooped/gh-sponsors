using Devlooped.SponsorLink;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;

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

// Provide the authenticated GH CLI user account via DI
var registrations = new ServiceCollection();
registrations.AddSingleton(account);
var registrar = new TypeRegistrar(registrations);

var app = new CommandApp<SyncCommand>(registrar);
app.Configure(config =>
{
    //config.AddCommand<ShowCommand>();
    config.AddCommand<ListCommand>().WithDescription("Lists user and organization sponsorships");
    config.AddCommand<SyncCommand>().WithDescription("Synchronizes the sponsorships manifest");
    config.AddCommand<ValidateCommand>().WithDescription("Validates the active sponsorships manifest, if any");

#if DEBUG
    //config.PropagateExceptions();
    config.ValidateExamples();
#endif
});

#if DEBUG
if (args.Length == 0)
{
    var command = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Command to run:")
            .AddChoices(new[]
            {
                "list",
                "sync",
                "validate"
            }));

    args = new[] { command };
}
#endif

return app.Run(args);
