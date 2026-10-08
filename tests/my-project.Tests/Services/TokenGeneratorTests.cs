using my_project.Services;

namespace my_project.Tests.Services;

public class TokenGeneratorTests
{
    [Fact]
    public void Carries_at_least_128_bits_of_entropy() // NFR-002, NFR-003
    {
        // 32 random bytes base64url-encoded without padding: 43 characters.
        Assert.Equal(43, TokenGenerator.Create().Length);
    }

    [Fact]
    public void Is_url_safe()
    {
        foreach (var token in Enumerable.Range(0, 200).Select(_ => TokenGenerator.Create()))
        {
            Assert.Matches("^[A-Za-z0-9_-]+$", token);
        }
    }

    [Fact]
    public void Does_not_repeat()
    {
        var tokens = Enumerable.Range(0, 1000).Select(_ => TokenGenerator.Create()).ToList();

        Assert.Equal(tokens.Count, tokens.Distinct().Count());
    }
}
