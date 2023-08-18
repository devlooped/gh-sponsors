using System.Text.Json;
using System.Text.Json.Serialization;

namespace Devlooped.SponsorLink;

public record Account([property: JsonPropertyName("node_id")] string Id, string Login);

public static class GitHub
{
    public static bool IsInstalled { get; } = TryIsInstalled(out var _);

    public static bool TryIsInstalled(out string output)
        => Process.TryExecute("gh", "--version", out output) && output.StartsWith("gh version");

    public static bool TryApi(string endpoint, string jq, out string? json)
    {
        var args = $"api {endpoint}";
        if (!string.IsNullOrEmpty(jq))
            args += $" --jq \"{jq}\"";

        return Process.TryExecute("gh", args, out json);
    }

    public static bool TryQuery(string query, string jq, out string? json)
    {
        var args = $"api graphql -f query=\"{query}\"";
        if (!string.IsNullOrEmpty(jq))
            args += $" --jq \"{jq}\"";

        return Process.TryExecute("gh", args, out json);
    }

    public static Account? Authenticate()
    {
        if (!Process.TryExecute("gh", "auth status -h github.com", out var output))
            return default;

        if (output.Contains("gh auth login"))
            return default;

        if (!Process.TryExecute("gh", "api user", out output))
            return default;

        return JsonSerializer.Deserialize<Account>(output, JsonOptions.Default);
    }
}
