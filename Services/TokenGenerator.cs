using System.Security.Cryptography;

namespace my_project.Services;

/// <summary>
/// Invite and session tokens: 256 bits from a cryptographically secure source, encoded
/// URL-safe so a token can sit in a path segment unescaped (NFR-002, NFR-003).
/// </summary>
public static class TokenGenerator
{
    public static string Create() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
