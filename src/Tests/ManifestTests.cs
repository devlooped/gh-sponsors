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


        var manifest = Manifest.Create(
            new[] { "foo@bar.com" },
            new[] { "bar.com", "baz.com" },
            new[] { "devlooped" },
            key);

        var validated = Manifest.Read(manifest.Token, pub);

        // Direct sponsoring
        Assert.True(validated.IsSponsoring("foo@bar.com", "devlooped"));
        // Org sponsoring (via sponsoring domain)
        Assert.True(validated.IsSponsoring("baz@bar.com", "devlooped"));

        // Wrong sponsorable
        Assert.False(validated.IsSponsoring("foo@bar.com", "dotnet"));
        // Wrong email domain
        Assert.False(validated.IsSponsoring("foo@contoso.com", "devlooped"));
    }

    [Fact]
    public void WrongPublicKey()
    {
        var key = RSA.Create();
        key.ImportRSAPrivateKey(File.ReadAllBytes(@"../../../test.key"), out _);

        var manifest = Manifest.Create(
            new[] { "foo@bar.com" },
            new[] { "bar.com", "baz.com" },
            new[] { "devlooped" },
            key);

        Assert.ThrowsAny<SecurityTokenInvalidSignatureException>(() => Manifest.Read(manifest.Token));
    }
}
