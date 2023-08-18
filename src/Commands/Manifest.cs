using System.IdentityModel.Tokens.Jwt;
using System.Numerics;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Devlooped.SponsorLink;

/// <summary>
/// Represents a manifest of sponsorship links.
/// </summary>
public class Manifest
{
    readonly HashSet<string> linked;

    static Manifest()
    {
        PublicKey = RSA.Create();
        using var stream = typeof(Manifest).Assembly
            .GetManifestResourceStream("Devlooped.SponsorLink.SponsorLink.pub");

        var mem = new MemoryStream((int)stream!.Length);
        stream.CopyTo(mem);

        PublicKey.ImportRSAPublicKey(mem.ToArray(), out _);
    }

    Manifest(string jwt, ClaimsPrincipal principal)
        : this(jwt, new HashSet<string>(principal.FindAll("sl").Select(x => x.Value))) { }

    Manifest(string jwt, HashSet<string> linked)
        => (Token, this.linked) = (jwt, linked);

    /// <summary>
    /// Checks whether the given email is sponsoring the given sponsorable account.
    /// </summary>
    public bool IsSponsoring(string email, string sponsorable)
        => linked.Contains(
                Base62.Encode(BigInteger.Abs(new BigInteger(
                    SHA256.HashData(Encoding.UTF8.GetBytes(email + sponsorable)))))) ||
            linked.Contains(
                Base62.Encode(BigInteger.Abs(new BigInteger(
                    SHA256.HashData(Encoding.UTF8.GetBytes(email[(email.IndexOf('@') + 1)..] + sponsorable))))));

    /// <summary>
    /// The JWT token representing the manifest.
    /// </summary>
    public string Token { get; }

    /// <summary>
    /// The public key used to validate manifests signed with the default private key.
    /// </summary>
    public static RSA PublicKey { get; }

    /// <summary>
    /// Reads a manifest and validates it using the embedded public key.
    /// </summary>
    public static Manifest Read(string token) => Read(token, PublicKey);

    /// <summary>
    /// Reads a manifest and validates it using the given public key.
    /// </summary>
    public static Manifest Read(string token, RSA rsa)
    {
        var validation = new TokenValidationParameters
        {
            RequireExpirationTime = true,
            ValidAudience = "SponsorLink",
            ValidIssuer = "Devlooped",
            IssuerSigningKey = new RsaSecurityKey(rsa)
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, validation, out var _);

        // For now, it's a single entry, with "is active sponsor" claim only. sl = sponsor-linked
        return new Manifest(token, principal);

        //try
        //{
        //}
        //catch (Exception ex) when (ex is SecurityTokenExpiredException || ex is SecurityTokenInvalidSignatureException)
        //{
        //}
    }

    /// <summary>
    /// Creates an unsigned manifest, to be used to request a signed one.
    /// </summary>
    public static Manifest Create(string[] emails, string[] domains, string[] sponsoring)
    {
        var linked = new HashSet<string>();

        foreach (var sponsorable in sponsoring)
        {
            foreach (var email in emails)
            {
                var data = SHA256.HashData(Encoding.UTF8.GetBytes(email + sponsorable));
                var hash = Base62.Encode(BigInteger.Abs(new BigInteger(data)));

                linked.Add(hash);
            }

            foreach (var domain in domains)
            {
                var data = SHA256.HashData(Encoding.UTF8.GetBytes(domain + sponsorable));
                var hash = Base62.Encode(BigInteger.Abs(new BigInteger(data)));

                linked.Add(hash);
            }
        }

        var token = new JwtSecurityToken(
            issuer: "Devlooped",
            audience: "SponsorLink",
            claims: linked.Select(x => new Claim("sl", x)),
            // Expire at the end of the month
            expires: new DateTime(DateTime.Today.Year, DateTime.Today.Month + 1, 1, 0, 0, 0, DateTimeKind.Utc));

        // Serialize the token and return as a string
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return new Manifest(jwt, linked);
    }

    /// <summary>
    /// Creates a signed manifest, to be used to verify sponsorships.
    /// </summary>
    public static Manifest Create(string[] emails, string[] domains, string[] sponsoring, RSA rsa)
    {
        var key = new RsaSecurityKey(rsa.ExportParameters(true));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        var linked = new HashSet<string>();

        foreach (var sponsorable in sponsoring)
        {
            foreach (var email in emails)
            {
                var data = SHA256.HashData(Encoding.UTF8.GetBytes(email + sponsorable));
                var hash = Base62.Encode(BigInteger.Abs(new BigInteger(data)));

                linked.Add(hash);
            }

            foreach (var domain in domains)
            {
                var data = SHA256.HashData(Encoding.UTF8.GetBytes(domain + sponsorable));
                var hash = Base62.Encode(BigInteger.Abs(new BigInteger(data)));

                linked.Add(hash);
            }
        }

        var token = new JwtSecurityToken(
            issuer: "Devlooped",
            audience: "SponsorLink",
            claims: linked.Select(x => new Claim("sl", x)),
            // Expire at the end of the month
            expires: new DateTime(DateTime.Today.Year, DateTime.Today.Month + 1, 1, 0, 0, 0, DateTimeKind.Utc),
            signingCredentials: credentials);

        // Serialize the token and return as a string
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return new Manifest(jwt, linked);
    }
}
