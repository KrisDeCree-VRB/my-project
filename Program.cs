using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using my_project.Data;
using my_project.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=weekendaway.db"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<VisitorIdentity>();
builder.Services.AddScoped<TripService>();

// NFR-004: invite-link lookups are rate-limited per client so the token space cannot be
// probed in bulk. Applied to the join page via [EnableRateLimiting("invite")].
// The limit is configurable only so the end-to-end suite can drive the join page harder
// than any real client would; the default is the value the deployed app runs on.
var invitePermitLimit = builder.Configuration.GetValue("RateLimiting:InvitePermitLimit", 20);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("invite", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = invitePermitLimit, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

// No migrations yet — the schema is created on first run. Replace with migrations once
// the model has to survive a shipped version.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// EC-5: with cookies disabled the antiforgery token cannot come back, and every form post
// fails. Turn that into a plain explanation rather than a bare 400.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (AntiforgeryValidationException)
    {
        context.Response.Redirect("/CookiesRequired");
    }
});

app.UseRateLimiter();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

/// <summary>Exposed so the integration tests can host the app with WebApplicationFactory.</summary>
public partial class Program;
