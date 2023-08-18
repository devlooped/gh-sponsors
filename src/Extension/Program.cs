using Devlooped.SponsorLink;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<ShowCommand>();
    config.AddCommand<ListCommand>();
    config.AddCommand<LinkCommand>();
    config.AddCommand<ValidateCommand>();

#if DEBUG
    //config.PropagateExceptions();
    config.ValidateExamples();
#endif
});

if (args.Length == 0)
{
    var command = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Command to run:")
            .AddChoices(new[]
            {
                "show", "list", "link", "validate"
            }));

    args = new[] { command };
}

return app.Run(args);
