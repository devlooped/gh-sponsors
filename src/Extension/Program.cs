using Devlooped.SponsorLink;
using Spectre.Console.Cli;

var app = new CommandApp<ListCommand>();
app.Configure(config =>
{
    config.AddCommand<ShowCommand>();

#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
});

return app.Run(args);
