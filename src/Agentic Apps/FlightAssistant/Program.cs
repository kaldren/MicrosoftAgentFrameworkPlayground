using Azure.Core;
using Azure.Identity;
using FlightAssistant.Models;
using FlightAssistant.Services;
using Npgsql;
using Testcontainers.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// ── PostgreSQL ───────────────────────────────────────────────
//   Development  (ConnectionStrings:Postgres absent or empty)
//     → Testcontainers spins up a postgres:17-alpine container automatically.
//       Docker must be running; no other setup is required.
//
//   Production   (ConnectionStrings:Postgres set — e.g. via Azure App Service
//                 "Connection strings" or an environment variable)
//     → Azure Database for PostgreSQL Flexible Server,
//       authenticated via Microsoft Entra ID with automatic token refresh.
var azurePgConnectionString = builder.Configuration.GetConnectionString("Postgres");
var isAzure = !string.IsNullOrEmpty(azurePgConnectionString);

PostgreSqlContainer? postgresContainer = null;

if (!isAzure)
{
    Console.WriteLine("Starting local PostgreSQL container...");
    postgresContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("flightassistant")
        .Build();
    await postgresContainer.StartAsync();
    Console.WriteLine("PostgreSQL container started.");
}

var pgConnectionString = isAzure
    ? azurePgConnectionString!
    : postgresContainer!.GetConnectionString();

// Build NpgsqlDataSource — with Entra ID token refresh for Azure,
// or plain connection string for the local container.
NpgsqlDataSource pgDataSource;

if (isAzure)
{
    var dsBuilder = new NpgsqlDataSourceBuilder(pgConnectionString);
    // UsePeriodicPasswordProvider refreshes the Entra ID token before it expires,
    // which is essential for long-running web apps.
    dsBuilder.UsePeriodicPasswordProvider(
        async (_, ct) =>
        {
            var token = await new AzureCliCredential().GetTokenAsync(
                new TokenRequestContext(["https://ossrdbms-aad.database.windows.net/.default"]), ct);
            return token.Token;
        },
        successRefreshInterval: TimeSpan.FromMinutes(50),
        failureRefreshInterval: TimeSpan.FromSeconds(10));
    pgDataSource = dsBuilder.Build();
}
else
{
    pgDataSource = new NpgsqlDataSourceBuilder(pgConnectionString).Build();
}

// Register as a singleton so pages can inject NpgsqlDataSource directly.
// The DI container disposes it on shutdown before ApplicationStopped fires,
// ensuring all connections are closed before the container is stopped.
builder.Services.AddSingleton(pgDataSource);
builder.Services.AddScoped<IPostgreService, PostgreService>();

// Initialise the database schema before the app starts accepting requests.
await InitialiseDatabaseAsync(pgDataSource);
await SeedDatabaseAsync(pgDataSource);

// Initialize the FlightAgent, which will be injected into Razor Pages that need it.
builder.Services.AddScoped<IFlightAgent, FlightAgent>();

var app = builder.Build();

// Stop and dispose the container after the host (and DI) has fully shut down.
if (postgresContainer is not null)
{
    app.Lifetime.ApplicationStopped.Register(() =>
        postgresContainer.DisposeAsync().AsTask().GetAwaiter().GetResult());
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();

// ── Local functions ──────────────────────────────────────────

static async Task InitialiseDatabaseAsync(NpgsqlDataSource dataSource)
{
    await using var conn = await dataSource.OpenConnectionAsync();
    await using var cmd  = conn.CreateCommand();
    cmd.CommandText = """
        CREATE TABLE IF NOT EXISTS flights (
            id          SERIAL  PRIMARY KEY,
            name        TEXT    NOT NULL,
            description TEXT    NOT NULL DEFAULT '',
            from_city   TEXT    NOT NULL,
            to_city     TEXT    NOT NULL,
            duration    DECIMAL NOT NULL,
            total_seats INT     NOT NULL,
            seats       INT     NOT NULL
        );
        """;
    await cmd.ExecuteNonQueryAsync();
}

static async Task SeedDatabaseAsync(NpgsqlDataSource dataSource)
{
    await using var conn = await dataSource.OpenConnectionAsync();
    await using var checkCmd = conn.CreateCommand();
    checkCmd.CommandText = "SELECT COUNT(*) FROM flights";
    var count = (long)(await checkCmd.ExecuteScalarAsync())!;
    if (count > 0) return;

    var seeds = new List<FlightModel>
    {
        new() { Name = "FA101", Description = "Morning express to New York",    From = "London", To = "New York",  Duration = 7.5m,  TotalSeats = 200, Seats = 45  },
        new() { Name = "FA202", Description = "Afternoon flight to Paris",       From = "London", To = "Paris",    Duration = 1.5m,  TotalSeats = 150, Seats = 120 },
        new() { Name = "FA303", Description = "Evening departure to Tokyo",      From = "London", To = "Tokyo",    Duration = 11.5m, TotalSeats = 300, Seats = 12  },
        new() { Name = "FA404", Description = "Direct flight to Sydney",         From = "London", To = "Sydney",   Duration = 21.5m, TotalSeats = 250, Seats = 80  },
        new() { Name = "FA505", Description = "Business class to Dubai",         From = "London", To = "Dubai",    Duration = 7.0m,  TotalSeats = 180, Seats = 30  },
        new() { Name = "FA606", Description = "Budget flight to Berlin",         From = "London", To = "Berlin",   Duration = 2.0m,  TotalSeats = 120, Seats = 95  },
    };

    await using var cmd = conn.CreateCommand();
    cmd.CommandText = """
        INSERT INTO flights (name, description, from_city, to_city, duration, total_seats, seats)
        VALUES (@name, @description, @from, @to, @duration, @totalSeats, @seats)
        """;
    var pName        = cmd.Parameters.Add("name",       NpgsqlTypes.NpgsqlDbType.Text);
    var pDescription = cmd.Parameters.Add("description", NpgsqlTypes.NpgsqlDbType.Text);
    var pFrom        = cmd.Parameters.Add("from",        NpgsqlTypes.NpgsqlDbType.Text);
    var pTo          = cmd.Parameters.Add("to",          NpgsqlTypes.NpgsqlDbType.Text);
    var pDuration    = cmd.Parameters.Add("duration",    NpgsqlTypes.NpgsqlDbType.Numeric);
    var pTotalSeats  = cmd.Parameters.Add("totalSeats",  NpgsqlTypes.NpgsqlDbType.Integer);
    var pSeats       = cmd.Parameters.Add("seats",       NpgsqlTypes.NpgsqlDbType.Integer);
    await cmd.PrepareAsync();

    foreach (var f in seeds)
    {
        pName.Value        = f.Name;
        pDescription.Value = f.Description;
        pFrom.Value        = f.From;
        pTo.Value          = f.To;
        pDuration.Value    = f.Duration;
        pTotalSeats.Value  = f.TotalSeats;
        pSeats.Value       = f.Seats;
        await cmd.ExecuteNonQueryAsync();
    }
}
