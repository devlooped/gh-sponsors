using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

namespace Devlooped.SponsorLink;

public class ListCommand : Command
{
    record Sponsorable(string Login);
    record Tier(int MonthlyPriceInDollars);
    record Sponsorship(DateTime CreatedAt, Sponsorable Sponsorable, Tier Tier);

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

        if (!GitHub.TryQuery(
                """
                query { 
                  viewer { 
                    sponsorshipsAsSponsor(first: 100, orderBy: {field: CREATED_AT, direction: ASC}) {
                      nodes {
                         createdAt
                         sponsorable {
                           ... on Organization {
                             login
                           }
                           ... on User {
                             login
                           }
                         }
                         tier {
                           monthlyPriceInDollars
                         }        
                      }
                    }
                  }
                }
                """, ".data.viewer.sponsorshipsAsSponsor.nodes", out var json))
            return -1;

        if (string.IsNullOrEmpty(json))
            return 0;

        var table = JsonSerializer.Deserialize<Sponsorship[]>(json, JsonOptions.Default)!
            .Select(x => new { x.Sponsorable.Login, x.Tier.MonthlyPriceInDollars, x.CreatedAt })
            .AsTable();

        AnsiConsole.Write(table);

        return 0;
    }
}
