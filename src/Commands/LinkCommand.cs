using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Devlooped.SponsorLink;

public partial class LinkCommand : GitHubCommand
{
    record Organization(string Email, string WebsiteUrl);

    protected override int OnExecute(Account user, CommandContext context)
    {
        if (!GitHub.TryQuery(
                """
                query { 
                  viewer { 
                    sponsorshipsAsSponsor(activeOnly: true, first: 100, orderBy: {field: CREATED_AT, direction: ASC}) {
                      nodes {
                         sponsorable {
                           ... on Organization {
                             login
                           }
                           ... on User {
                             login
                           }
                         }        
                      }
                    }
                  }
                }
                """,
                """
                [.data.viewer.sponsorshipsAsSponsor.nodes.[].sponsorable.login]
                """, out var json))
            return -1;

        if (string.IsNullOrEmpty(json))
            return 0;

        var sponsoring = JsonSerializer.Deserialize<string[]>(json, JsonOptions.Default) ?? Array.Empty<string>();
        if (sponsoring.Length == 0)
        {
            AnsiConsole.WriteLine($"[yellow]User {user.Login} is not currently sponsoring any accounts.");
            return 0;
        }

        if (!GitHub.TryQuery(
            """
            query { 
              viewer { 
                organizations(first: 100) {
                  nodes {
                    isVerified
                    email
                    websiteUrl
                  }
                }
              }
            }
            """,
            """
            [.data.viewer.organizations.nodes.[] | select(.isVerified == true)]
            """, out json) || json is null)
            return -1;

        var orgs = JsonSerializer.Deserialize<Organization[]>(json, JsonOptions.Default) ?? Array.Empty<Organization>();
        var domains = new HashSet<string>();
        // Collect unique domains from verified org website and email
        foreach (var org in orgs)
        {
            // NOTE: should we automatically also collect subdomains?
            if (Uri.TryCreate(org.WebsiteUrl, UriKind.Absolute, out var uri))
                domains.Add(uri.Host);

            if (string.IsNullOrEmpty(org.Email))
                continue;

            var domain = org.Email.Split('@')[1];
            if (string.IsNullOrEmpty(domain))
                continue;

            domains.Add(domain);
        }

        if (!GitHub.TryApi("user/emails", "[.[] | select(.verified == true) | .email]", out json) || json is null)
            return -1;

        var emails = JsonSerializer.Deserialize<string[]>(json, JsonOptions.Default) ?? Array.Empty<string>();
        // Create unsigned manifest locally, for back-end validation
        var manifest = Manifest.Create(emails, domains.ToArray(), sponsoring);

        // Send token to API to get signed manifest and persist it locally
        File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".sponsorlink"), manifest.Token);

        return 0;
    }
}
