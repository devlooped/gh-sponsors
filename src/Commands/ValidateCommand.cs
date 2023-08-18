using Microsoft.IdentityModel.Tokens;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Devlooped.SponsorLink;

public partial class ValidateCommand : GitHubCommand
{
    protected override int OnExecute(Account user, CommandContext context)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".sponsorlink");
        if (!File.Exists(path))
        {
            AnsiConsole.MarkupLine("[red]No SponsorLink file found in your home directory.[/] Run [white]gh sponsors link[/] to initialize it.");
            return -1;
        }

        try
        {
            Manifest.Read(File.ReadAllText(path));
        }
        catch (SecurityTokenExpiredException)
        {
            AnsiConsole.MarkupLine("[red]The manifest has expired.[/] Run [white]gh sponsors link[/] to generate a new one.");
            return -2;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            AnsiConsole.MarkupLine("[red]The manifest signature is invalid.[/] Run [white]gh sponsors link[/] to generate a new one.");
            return -3;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]The manifest is invalid.[/] Run [white]gh sponsors link[/] to generate a new one.");
            AnsiConsole.WriteException(ex);
            return -4;
        }

        AnsiConsole.MarkupLine("[green]The manifest is valid.[/]");
        return 0;
    }
}
