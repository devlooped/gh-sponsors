using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using static Devlooped.SponsorLink;

namespace Devlooped.Sponsors;

[Description("Synchronizes the sponsorships manifest")]
public partial class SyncCommand(Account user) : AsyncCommand
{
    record Organization(string Login, string Email, string WebsiteUrl);
    record OrgSponsor(string Login, string[] Sponsorables);

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var status = AnsiConsole.Status();
        string? json = default;

        // Authenticated user must match GH user
        var principal = await Session.AuthenticateAsync();
        if (principal == null)
            return -1;

        if (!int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Split('|')?[1], out var id))
        {
            AnsiConsole.MarkupLine("[red]x[/] Could not determine authenticated user id.");
            return -1;
        }

        if (user.Id != id)
        {
            AnsiConsole.MarkupLine($"[red]x[/] SponsorLink authenticated user id ({id}) does not match GitHub CLI user id ({user.Id}).");
            return -1;
        }

        // TODO: we'll need to account for pagination after 100 sponsorships is commonplace :)
        if (status.Start("Querying user sponsorships", _ =>
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
                """, out json) || string.IsNullOrEmpty(json))
            {
                AnsiConsole.MarkupLine("[red]Could not query GitHub for user sponsorships.");
                return -1;
            }
            return 0;
        }) == -1)
        {
            return -1;
        }

        var usersponsored = JsonSerializer.Deserialize<HashSet<string>>(json!, JsonOptions.Default) ?? new HashSet<string>();

        // It's unlikely that any account would belong to more than 100 orgs.
        if (status.Start("Querying user organizations", _ =>
        {
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
            return 0;
        }) == -1)
        {
            return -1;
        }

        var orgs = JsonSerializer.Deserialize<Organization[]>(json!, JsonOptions.Default) ?? Array.Empty<Organization>();
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
        var orgsponsors = new List<OrgSponsor>();

        // Collect org-sponsored accounts. NOTE: these must be public sponsorships 
        // since the current user would typically NOT be an admin of these orgs.
        status.Start("Querying organization sponsorships", ctx =>
        {
            foreach (var org in orgs)
            {
                ctx.Status($"Querying {org.Login} sponsorships");

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
                    JsonSerializer.Deserialize<string[]>(json, JsonOptions.Default) is { } sponsored &&
                    sponsored.Length > 0)
                {
                    orgsponsors.Add(new OrgSponsor(org.Login, sponsored));
                    foreach (var login in sponsored)
                    {
                        orgsponsored.Add(login);
                    }
                }
            }
        });

        // If we end up with no sponsorships whatesover, no-op and exit.
        if (usersponsored.Count == 0 && orgsponsored.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]![/] User {user.Login} (or any of the organizations they long to) is not currently sponsoring any accounts.");
            return 0;
        }

        AnsiConsole.MarkupLine($"[green]✓[/] Found {usersponsored.Count} personal sponsorships and {orgsponsored.Count} organization sponsorships.");

        // Create unsigned manifest locally, for back-end validation
        var manifest = Manifest.Create(Session.InstallationId, user.Id.ToString(), user.Emails, domains.ToArray(),
            new HashSet<string>(usersponsored.Concat(orgsponsored)).ToArray());

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Variables.AccessToken);

        // NOTE: to test the local flow end to end, run the SponsorLink functions App project locally. You will 
        var url = Debugger.IsAttached ? "http://localhost:7288/sign" : "https://sponsorlink.devlooped.com/sign";

        var response = await status.StartAsync(ThisAssembly.Strings.Sync.Signing, async _
            => await http.PostAsync(url, new StringContent(manifest.Token)));

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            AnsiConsole.MarkupLine("[red]x[/] Could not sign manifest: unauthorized.");
            return -1;
        }
        else if (!response.IsSuccessStatusCode)
        {
            AnsiConsole.MarkupLine($"[red]x[/] Could not sign manifest: {response.StatusCode} ({await response.Content.ReadAsStringAsync()}).");
            return -1;
        }

        var signed = await response.Content.ReadAsStringAsync();
        var signedManifest = Manifest.Read(signed, Session.InstallationId);

        // Make sure we can read it back
        Debug.Assert(manifest.Hashes.SequenceEqual(signedManifest.Hashes));

        AnsiConsole.MarkupLine($"[green]✓[/] Got signed manifest, expires on {signedManifest.ExpiresAt:yyyy-MM-dd}.");

        Variables.Manifest = signed;

        var tree = new Tree(new Text(Emoji.Replace(":purple_heart: Sponsorships"), new Style(Color.MediumPurple2)));

        if (usersponsored != null)
        {
            var node = new TreeNode(new Text(user.Login, new Style(Color.Blue)));
            node.AddNodes(usersponsored);
            tree.AddNode(node);
        }

        foreach (var org in orgsponsors)
        {
            var node = new TreeNode(new Text(org.Login, new Style(Color.Green)));
            node.AddNodes(org.Sponsorables);
            tree.AddNode(node);
        }

        AnsiConsole.Write(tree);

        return 0;
    }
}
