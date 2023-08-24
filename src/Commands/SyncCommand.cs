using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Devlooped.SponsorLink;

public partial class SyncCommand(Account user) : AsyncCommand
{
    record Organization(string Login, string Email, string WebsiteUrl);

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        // Authenticated user must match GH user
        var principal = await Session.AuthenticateAsync();
        if (!int.TryParse(principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value.Split('|')?[1], out var id))
        {
            AnsiConsole.MarkupLine("[red]Could not determine SponsorLink user id.");
            return -1;
        }

        if (user.Id != id)
        {
            AnsiConsole.MarkupLine($"[red]SponsorLink authenticated user id ({id}) does not match GitHub CLI user id ({user.Id}).");
            return -1;
        }

        // TODO: we'll need to account for pagination after 100 sponsorships is commonplace :)
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
                """, out var json) || string.IsNullOrEmpty(json))
        {
            AnsiConsole.MarkupLine("[red]Could not query GitHub for user sponsorships.");
            return -1;
        }

        var usersponsored = JsonSerializer.Deserialize<HashSet<string>>(json, JsonOptions.Default) ?? new HashSet<string>();

        // It's unlikely that any account would belong to more than 100 orgs.
        if (!GitHub.TryQuery(
            """
            query { 
              viewer { 
                organizations(first: 100) {
                  nodes {
                    login
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
            """, out json) || string.IsNullOrEmpty(json))
        {
            AnsiConsole.MarkupLine("[red]Could not query GitHub for user organizations.");
            return -1;
        }

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

        var orgsponsored = new HashSet<string>();

        // Collect org-sponsored accounts. NOTE: these must be public sponsorships 
        // since the current user would typically NOT be an admin of these orgs.
        foreach (var org in orgs)
        {
            // TODO: we'll need to account for pagination after 100 sponsorships is commonplace :)
            if (GitHub.TryQuery(
                $$"""
                query($login: String!) { 
                  organization(login: $login) { 
                    sponsorshipsAsSponsor(activeOnly: true, first: 100) {
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
                [.data.organization.sponsorshipsAsSponsor.nodes.[].sponsorable.login]
                """, out json, ("login", org.Login)) &&
                !string.IsNullOrEmpty(json) &&
                JsonSerializer.Deserialize<string[]>(json, JsonOptions.Default) is { } sponsored)
            {
                foreach (var login in sponsored)
                {
                    orgsponsored.Add(login);
                }
            }
        }

        // If we end up with no sponsorships whatesover, no-op and exit.
        if (usersponsored.Count == 0 && orgsponsored.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]User {user.Login} (or any of the organizations they long to) is not currently sponsoring any accounts.");
            return 0;
        }

        AnsiConsole.MarkupLine($"[grey]Found {usersponsored.Count} personal sponsorships and {orgsponsored.Count} organization sponsorships.[/]");

        // Create unsigned manifest locally, for back-end validation
        var manifest = Manifest.Create(Session.InstallationId, user.Id.ToString(), user.Emails, domains.ToArray(),
            new HashSet<string>(usersponsored.Concat(orgsponsored)).ToArray());

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Session.AccessToken);

        // NOTE: to test the local flow end to end, run the SponsorLink functions App project locally. You will 
        var url = Debugger.IsAttached ? "http://localhost:7288/sign" : "https://sponsorlink.devlooped.com/sign";
        var response = await http.PostAsync(url, new StringContent(manifest.Token));

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            AnsiConsole.MarkupLine("[red]Could not sign manifest: unauthorized.[/]");
            return -1;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Request installing GH SponsorLink App (records acceptance of email sharing).
            AnsiConsole.MarkupLine("[red]Could not sign manifest: not found.[/]");
            return -1;
        }
        else if (!response.IsSuccessStatusCode)
        {
            AnsiConsole.MarkupLine($"[red]Could not sign manifest: {response.StatusCode} ({await response.Content.ReadAsStringAsync()}).[/]");
            return -1;
        }

        var signed = await response.Content.ReadAsStringAsync();

        // Make sure we can read it back
        Debug.Assert(manifest.Hashes.SequenceEqual(Manifest.Read(signed, Session.InstallationId).Hashes));

        Environment.SetEnvironmentVariable("SPONSORLINK_MANIFEST", signed, EnvironmentVariableTarget.User);

        return 0;
    }
}
