using Devlooped.SponsorLink;
using Spectre.Console.Cli;

var app = new CommandApp<ShowCommand>();
app.Configure(config =>
{
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
});

return app.Run(args);
