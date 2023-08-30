using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
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

        var mem = new MemoryStream();
        var writer = new Utf8JsonWriter(mem);
        writer.WriteStartObject();

        var doc = JsonDocument.Parse(json);
        // Cleanup a bit by removing the raw and encoded properties which are 
        // already included in the token as human readable values already.
        var raw = doc.RootElement.EnumerateObject().Where(x =>
            !x.Name.StartsWith("raw") && !x.Name.StartsWith("encoded"));

        foreach (var node in raw)
            node.WriteTo(writer);

        writer.WriteEndObject();
        writer.Flush();
        mem.Position = 0;

        AnsiConsole.Write(
            new Panel(new JsonText(Encoding.UTF8.GetString(mem.ToArray())))
                .Header("Auth0 Token")
                .Collapse()
                .RoundedBorder()
                .BorderColor(Color.Green));

        return 0;
    }
}
