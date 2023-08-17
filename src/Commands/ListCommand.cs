using System.ComponentModel;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Devlooped.SponsorLink;

public class ListCommand : GitHubCommand
{
    record Sponsorship(string Sponsorable, [property: DisplayName("Tier (USD)")] int Dollars, DateOnly CreatedAt, [property: DisplayName("One-time")] bool OneTime);

    protected override int OnExecute(Account account, CommandContext context)
    {
        if (!GitHub.TryQuery(
                """
                query { 
                  viewer { 
                    sponsorshipsAsSponsor(first: 100, orderBy: {field: CREATED_AT, direction: ASC}) {
                      nodes {
                         createdAt
                         isOneTimePayment
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
                """,
                """
                [.data.viewer.sponsorshipsAsSponsor.nodes.[] | { sponsorable: .sponsorable.login, dollars: .tier.monthlyPriceInDollars, oneTime: .isOneTimePayment, createdAt } ]
                """, out var json))
            return -1;

        if (string.IsNullOrEmpty(json))
            return 0;

        var table = JsonSerializer.Deserialize<Sponsorship[]>(json, JsonOptions.Default)!
            .AsTable();

        AnsiConsole.Write(table);

        return 0;
    }
}
