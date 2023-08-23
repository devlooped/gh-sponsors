using Microsoft.IdentityModel.Tokens;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Devlooped.SponsorLink;

public partial class ValidateCommand : Command
{
    public override int Execute(CommandContext context)
    {
        var token = Environment.GetEnvironmentVariable("SPONSORLINK_MANIFEST", EnvironmentVariableTarget.User);
        if (string.IsNullOrEmpty(token))
        {
            AnsiConsole.MarkupLine("[red]No SponsorLink manifest found.[/] Run [white]gh sponsors sync[/] to initialize it.");
            return -1;
        }

        try
        {
            Manifest.Read(token);
        }
        catch (SecurityTokenExpiredException)
        {
            AnsiConsole.MarkupLine("[red]The manifest has expired.[/] Run [white]gh sponsors sync[/] to generate a new one.");
            return -2;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            AnsiConsole.MarkupLine("[red]The manifest signature is invalid.[/] Run [white]gh sponsors sync[/] to generate a new one.");
            return -3;
        }
        catch (SecurityTokenException ex)
        {
            AnsiConsole.MarkupLine($"[red]The manifest is invalid.[/] Run [white]gh sponsors sync[/] to generate a new one.");
            AnsiConsole.WriteException(ex);
            return -4;
        }

        AnsiConsole.MarkupLine("[green]The manifest is valid.[/]");
        return 0;
    }
}
