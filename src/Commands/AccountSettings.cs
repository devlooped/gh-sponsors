using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Devlooped.SponsorLink;

public partial class LinkCommand
{
    public class AccountSettings(string account) : CommandSettings
    {
        [Description("The account you are sponsoring.")]
        [CommandArgument(0, "<account>")]
        public string Account { get; init; } = account;

        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(Account))
                return ValidationResult.Error("Account is required.");

            if (GitHub.IsInstalled &&
                GitHub.Authenticate() is { } &&
                // If we are authenticated but can't get the acccount login from either org nor users
                !GitHub.TryApi($"orgs/{Account}", ".login", out _) &&
                !GitHub.TryApi($"users/{Account}", ".login", out _))
                return ValidationResult.Error($"Specified account '{Account}' does not exist on GitHub. See https://github.com/{Account}.");

            return ValidationResult.Success();
        }
    }
}
