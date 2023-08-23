using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Devlooped.SponsorLink;

public class ManifestTests(ITestOutputHelper Output)
{
    [Fact]
    public void EndToEnd()
    {
        var key = RSA.Create();
        key.ImportRSAPrivateKey(File.ReadAllBytes(@"../../../test.key"), out _);

        var pub = RSA.Create();
        pub.ImportRSAPublicKey(File.ReadAllBytes(@"../../../test.pub"), out _);

        var salt = Guid.NewGuid().ToString("N");

        var manifest = Manifest.Create(salt, "1234",
            // user email(s)
            new[] { "foo@bar.com" },
            // org domains
            new[] { "bar.com", "baz.com" },
            // sponsorables
            new[] { "devlooped" });

        // Turn it into a signed manifest
        var signed = manifest.Sign(key);

        var validated = Manifest.Read(signed, salt, pub);

        // Direct sponsoring
        Assert.True(validated.IsSponsoring("foo@bar.com", "devlooped"));
        // Org sponsoring (via sponsoring domain)
        Assert.True(validated.IsSponsoring("baz@bar.com", "devlooped"));

        // Wrong sponsorable
        Assert.False(validated.IsSponsoring("foo@bar.com", "dotnet"));
        // Wrong email domain
        Assert.False(validated.IsSponsoring("foo@contoso.com", "devlooped"));
    }
}
