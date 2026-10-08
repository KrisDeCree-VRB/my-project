using Microsoft.EntityFrameworkCore;
using my_project.Data;
using my_project.Domain;

namespace my_project.Services;

/// <summary>
/// Visitor identity: the cookie that names the browser, and the lookup from that cookie
/// to the <see cref="Visitor"/> holding this browser's bindings (FR-019).
/// </summary>
public class VisitorIdentity(AppDbContext db, IHttpContextAccessor httpContextAccessor)
{
    public const string CookieName = "wa_visitor";

    /// <summary>Visitor identities survive 90 days of inactivity (NFR-007).</summary>
    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(90);

    private Visitor? _cached;

    /// <summary>
    /// The visitor this browser already is, or null if it has none. A cookie naming a
    /// visitor that no longer exists reads as "no visitor" rather than an error (EC-7).
    /// </summary>
    public async Task<Visitor?> GetCurrentAsync()
    {
        if (_cached is not null) return _cached;

        var http = httpContextAccessor.HttpContext;
        if (http is null) return null;
        if (!http.Request.Cookies.TryGetValue(CookieName, out var token) || string.IsNullOrEmpty(token))
            return null;

        _cached = await db.Visitors
            .Include(v => v.Bindings).ThenInclude(b => b.Participant)
            .SingleOrDefaultAsync(v => v.SessionToken == token);

        _cached?.Touch();
        return _cached;
    }

    /// <summary>
    /// The visitor for this browser, establishing one if needed. Only called on actions
    /// that actually join or create — a mere invite preview must not mint an identity,
    /// so a chat-app link unfurl leaves no trace (EC-12).
    /// </summary>
    public async Task<Visitor> GetOrCreateAsync()
    {
        var existing = await GetCurrentAsync();
        if (existing is not null) return existing;

        var visitor = new Visitor(TokenGenerator.Create());
        db.Visitors.Add(visitor);

        httpContextAccessor.HttpContext?.Response.Cookies.Append(CookieName, visitor.SessionToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.Add(CookieLifetime),
        });

        _cached = visitor;
        return visitor;
    }

    /// <summary>True when the browser sent back a cookie we set earlier (EC-5).</summary>
    public bool BrowserSentCookie() =>
        httpContextAccessor.HttpContext?.Request.Cookies.ContainsKey(CookieName) == true;
}
