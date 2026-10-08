using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using my_project.Data;

namespace my_project.Tests.Integration;

/// <summary>
/// The whole app in-process, backed by a private in-memory SQLite database. SQLite rather
/// than the in-memory EF provider because the per-trip unique index on display names is
/// part of what these tests check (INV-3, EC-1).
/// </summary>
public class WeekendAwayApp : WebApplicationFactory<Program>
{
    private readonly DbConnection _connection = new SqliteConnection("Filename=:memory:");

    public WeekendAwayApp() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));
        });
    }

    /// <summary>
    /// One browser: its own cookie jar, so two devices are two clients. The https base
    /// address matters — the visitor cookie is <c>Secure</c>, so a client talking http
    /// would silently never send it back (NFR-003).
    /// </summary>
    public HttpClient NewDevice() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
    });

    public AppDbContext NewDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        return new AppDbContext(options);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}
