# GitHub CLI Sponsors Extension

An extension to the [GitHub CLI](https://cli.github.com/) to manage your 
sponsorships, sync your [SponsorLink](https://github.com/devlooped/SponsorLink) 
manifest and more.

## Installation

The [GitHub CLI](https://cli.github.com/) must be installed first. 

Install:

```shell
gh extension install devlooped/gh-sponsors
```

Upgrade:

```shell
gh extension upgrade devlooped/gh-sponsors
```

Uninstall:

```shell
gh extension remove devlooped/gh-sponsors
```

The extension uses the [GitHub CLI API](https://cli.github.com/manual/gh_api) to 
issue sponsors-related queries and render them to the console.


## Usage

The extension adds a `sponsors` command to the `gh` CLI, which you can use to
manage your sponsorships and sync your SponsorLink manifest.

Available commands: 

* `gh sponsors check <user/org> <sponsorable>`: checks if the current manifest 
  contains an active sponsorship from the specified user or organization to the 
  given sponsorable account.
* `gh sponsors list`: list your current sponsorships.
* `gh sponsors sync`: generate and sign your SponsorLink manifest. It requires 
  signing-in with your GitHub account both on the CLI and on github.com to authenticate 
  with SponsorLink backend for signing.
* `gh sponsors validate`: checks if the current manifest is valid and not expired.

> NOTE: running `gh sponsors` invokes the `sync` command.


## How it works

As a GitHub CLI extension, SponsorLink leverages your currently authenticated user 
to locate your personal sponsorships, as well as your organizations'. It does this 
entirely locally, communicating exclusively with the GitHub API itself. 

For example, it runs the following GraphQL query to retrieve your active sponsorships:

```graphql
query { 
    viewer { 
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
```

You can run these same queries on the [GitHub GraphQL Explorer](https://docs.github.com/en/graphql/overview/explorer).

This information is used to locally create a [JWT token](https://jwt.io/) with 
[hash claims](https://learn.microsoft.com/en-us/dotnet/api/system.security.claims.claimtypes.hash?view=net-7.0) 
calculated as: `base64(sha256(salt+user/org+sponsored))`.

The random salt is initialized to a GUID the first time you run this extension. 
Once hashed, the values are entirely opaque. Packages can use this JWT manifest 
to check (entirely offline) for hashed claims matching the salt + current user + 
sponsorable account to check for.

When the manifest is generated or synchronized (manually and explicitly when the 
`gh sponsors` command is run), it is sent our backend 
([open source at SponsorLink](https://github.com/devlooped/SponsorLink)) 
purely for signing with SponsorLink's private key. This allows consumers to verify 
(offline) the integrity of the manifest, even though it doesn't convey any private 
information.

The only bit of personally identifiable information that is sent to our backend with 
the JWT token is the GitHub user identifier (an integer like `169707`) which you authorize 
as part of authenticating with your GitHub account to our backend API (we use Auth0 for this). 
This identifier is already public for everyone on GitHub (i.e. see 
[dependabot-bot](https://api.github.com/users/dependabot-bot) or 
[dependabot org](https://api.github.com/orgs/dependabot)).

Please read the full [privacy policy](privacy.md) for more information.

### Implementation details

After the first sync (and accepting the usage terms), the following environment 
variables are populated:

* `SPONSORLINK_INSTALLATION`: a random GUID used for salting hashes.
* `SPONSORLINK_MANIFEST`: the JWT token with hashed claims for your current 
  sponsorships.
* `SPONSORLINK_TOKEN`: last used access token to invoke SponsorLink backend 
  JWT signing API. Issued by Auth0 after authenticating with your GitHub account 
  on github.com.

Public key for validating the manifest (paste in one-line): 

```text
MIIBCgKCAQEAo5bLk9Iim5twrxGVzJ4OxfxDuvy3ladTNvcFNv4Hm9/1No89SISKTXZ1bSABTnq
H6z/DpklcHveGMSmsncEvUebrg7tX6+M3byVXU6Q/d82PtwgbDXT9d10A4lePS2ioJQqlHWQy/f
uNwe7FjptV+yguf5IUxVRdZ77An1IyGUk9Cj6n4RuYIPrP5O0AmFPHOwEzywUWVaV1NHYRe0Th6
i5/hyDV13K7+LP9VzwucnWEvzujtnL6ywZDeaKkwfeFsXZyYywHj6oJK9Obed/nu1e+69fmUqpr
tc0t/3A9uHc0G/0sDNLLAd83j2NSOS2IHJo17azOLFuhekka8dSKnQIDAQAB
```
