using System.Net;
using System.Text.RegularExpressions;

namespace my_project.Tests.Integration;

/// <summary>
/// The little bit of browser behaviour the tests need: read a page, fill in its form,
/// post it back with the antiforgery token the server put in it.
/// </summary>
public static partial class BrowserExtensions
{
    public static async Task<string> GetHtmlAsync(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>Submits a form on <paramref name="url"/> with the given fields.</summary>
    public static async Task<HttpResponseMessage> SubmitFormAsync(
        this HttpClient client, string url, string postUrl, Dictionary<string, string> fields)
    {
        var html = await client.GetHtmlAsync(url);
        return await client.PostFormAsync(postUrl, html, fields);
    }

    /// <summary>Posts a form using the antiforgery token found in already-fetched HTML.</summary>
    public static async Task<HttpResponseMessage> PostFormAsync(
        this HttpClient client, string postUrl, string html, Dictionary<string, string> fields)
    {
        var all = new Dictionary<string, string>(fields)
        {
            ["__RequestVerificationToken"] = AntiforgeryToken(html),
        };

        return await client.PostAsync(postUrl, new FormUrlEncodedContent(all));
    }

    public static string AntiforgeryToken(string html)
    {
        var match = AntiforgeryTokenPattern().Match(html);
        Assert.True(match.Success, "No antiforgery token found on the page.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    /// <summary>The invite link as a participant sees it on the trip page.</summary>
    public static string? InviteUrl(string tripPageHtml)
    {
        var match = InviteUrlPattern().Match(tripPageHtml);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    /// <summary>The path the invite link points at, e.g. <c>/join/abc</c>.</summary>
    public static string? InvitePath(string tripPageHtml) =>
        InviteUrl(tripPageHtml) is { } url ? new Uri(url).PathAndQuery : null;

    [GeneratedRegex("""name="__RequestVerificationToken"[^>]*value="([^"]+)""")]
    private static partial Regex AntiforgeryTokenPattern();

    [GeneratedRegex("""id="inviteUrl"[^>]*value="([^"]*)""")]
    private static partial Regex InviteUrlPattern();
}
