using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;
using static Devlooped.SponsorLink;

namespace Devlooped.Sponsors;

[Description("View the information the backend contains about the authenticated user")]
public class ViewCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        if (Variables.AccessToken is string token)
        {
            return Render(token);
        }
        else
        {
            // Authenticated user must match GH user
            var principal = await Session.AuthenticateAsync();
            if (principal == null)
                return -1;

           return Render(Variables.AccessToken);
        }
    }

    int Render(string? token)
    {
        if (token is null)
            return -1;

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        var json = JsonSerializer.Serialize(jwt, JsonOptions.Default);

        AnsiConsole.Write(
            new Panel(new JsonText(json))
                .Header("Auth0 Token")
                .Collapse()
                .RoundedBorder()
                .BorderColor(Color.Green));

        return 0;
    }
}
