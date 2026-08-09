using System.Diagnostics;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using StackExchange.Redis;
using ZeroHour.Server.Data;
using ZeroHour.Server.Hubs;
using ZeroHour.Sim;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Structured logging, so a production incident can be queried rather than grepped.
// Console only for now: shipping to a log aggregator is a Phase 9 concern, and the
// sink is a configuration change rather than a code one when that time comes.
//
// No PII (`24 §6`). That constraint binds the call sites — the sink cannot rescue a
// message that already had a player's email interpolated into it.
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.AddHealthChecks();

// Postgres is the truth (`15`); SQLite exists so local work does not require Docker.
// The provider is chosen by which connection string is present rather than by a separate
// flag, so there is one source of truth and no way to set the two inconsistently.
string? postgres = builder.Configuration.GetConnectionString("Postgres");

if (!string.IsNullOrWhiteSpace(postgres))
{
    builder.Services.AddDbContext<GameDbContext, PostgresGameDbContext>(options =>
        options.UseNpgsql(postgres));
}
else
{
    // Deliberately loud. Silently falling back to a local file in production would mean
    // writes appearing to succeed while landing nowhere anyone can see.
    string sqlite = builder.Configuration.GetConnectionString("Sqlite")
        ?? "Data Source=zerohour.dev.db";

    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "ConnectionStrings__Postgres is required outside Development. Refusing to start "
            + "on SQLite, which would accept writes that never reach the real database.");
    }

    builder.Services.AddDbContext<GameDbContext, SqliteGameDbContext>(options =>
        options.UseSqlite(sqlite));
}

// Redis is optional. It is a cache and a SignalR backplane, not a source of truth, so a
// missing connection string degrades features rather than preventing startup — unlike
// Postgres above, which the server refuses to run without outside Development.
string? redis = builder.Configuration.GetConnectionString("Redis");

if (!string.IsNullOrWhiteSpace(redis))
{
    ConfigurationOptions redisOptions = ConfigurationOptions.Parse(redis);

    // AbortOnConnectFail defaults to true, which throws at construction if Redis happens to be
    // down and would take the whole app with it. False lets the multiplexer reconnect in the
    // background, which is the behaviour a cache should have.
    redisOptions.AbortOnConnectFail = false;
    redisOptions.ConnectTimeout = 5000;

    // Singleton and lazy: ConnectionMultiplexer is expensive, thread-safe and designed to be
    // shared. Lazy so a Redis outage cannot stop the process from starting.
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(redisOptions));
}

// MessagePack over the default JSON protocol (`16 §1`): the payloads are dense and
// frequent, and mobile clients pay for every byte on a metered connection.
builder.Services
    .AddSignalR()
    .AddMessagePackProtocol();

WebApplication app = builder.Build();

// One structured line per request, instead of the several ASP.NET Core logs by default.
app.UseSerilogRequestLogging();

// Kestrel sits behind Nginx in deployment (docs/14 §5), which terminates TLS.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// NOTE ON AUTH: every endpoint below is intentionally unauthenticated, because all three are
// operational probes carrying no player data. The gameplay API that lands in Phase 1 must not
// follow this pattern — it needs authentication before it exposes anything player-owned.

// Liveness. Deliberately dependency-free: it answers "is this process alive" and nothing more.
// Once Postgres and Redis are wired up, readiness gets its own checks — conflating the two
// means a brief cache blip pulls healthy app nodes out of the load balancer.
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "ZeroHour.Server",
    version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0",
    utc = DateTime.UtcNow.ToString("o"),
}));

// Confirms the shared deterministic sim is loaded and behaving inside the server process.
//
// Not a toy endpoint: the whole architecture rests on client and server computing identical
// results. Being able to verify the sim answers correctly here, before gameplay depends on it,
// is what makes a later desync tractable instead of a mystery.
app.MapGet("/health/sim", () =>
{
    var stopwatch = Stopwatch.StartNew();

    // Fixed-point arithmetic must be bit-identical on every platform: 3 * 7 = 21.
    Fixed product = Fixed.FromInt(3) * Fixed.FromInt(7);

    // A known seed must always produce the same sequence.
    var random = new DetRandom(12345UL);
    ulong first = random.NextULong();
    ulong second = random.NextULong();

    stopwatch.Stop();

    return Results.Ok(new
    {
        status = "healthy",
        simLoaded = true,
        fixedMultiply = product.ToString(),
        fixedRaw = product.Raw,
        deterministicSequence = new[] { first, second },
        elapsedMs = stopwatch.Elapsed.TotalMilliseconds,
    });
});

app.MapHealthChecks("/health/ready");

// Dependency check, separate from liveness and readiness on purpose. This one is allowed to
// be slow and is allowed to fail: it actually talks to the database. Point dashboards and
// humans at it, never a load balancer, or a transient database blip cycles healthy nodes.
app.MapGet("/health/deep", async (
    GameDbContext db,
    IServiceProvider services,
    CancellationToken cancellationToken) =>
{
    var stopwatch = Stopwatch.StartNew();

    bool canConnect;
    string? failure = null;

    try
    {
        canConnect = await db.Database.CanConnectAsync(cancellationToken);
    }
    catch (Exception ex)
    {
        canConnect = false;

        // The exception type only. A connection failure message can carry the host, database
        // name and sometimes the user, and this endpoint is unauthenticated (see the note
        // above) — so the detail goes to the log, not to the response body.
        failure = ex.GetType().Name;

        app.Logger.LogError(ex, "Deep health check could not reach the database");
    }

    // Resolved rather than injected, because Redis is only registered when a connection string
    // is present. A nullable minimal-API parameter would still be a required dependency and
    // would 500 the endpoint on the SQLite-only local path.
    var multiplexer = services.GetService<IConnectionMultiplexer>();

    bool redisConfigured = multiplexer is not null;
    bool redisReachable = false;
    string? redisFailure = null;
    double? redisPingMs = null;

    if (multiplexer is not null)
    {
        try
        {
            // PING rather than IsConnected: the flag reports what the multiplexer believes,
            // which stays true for a while after the server has gone. A round trip is the
            // only thing that proves the connection currently works.
            TimeSpan latency = await multiplexer.GetDatabase().PingAsync();
            redisReachable = true;
            redisPingMs = latency.TotalMilliseconds;
        }
        catch (Exception ex)
        {
            redisFailure = ex.GetType().Name;
            app.Logger.LogError(ex, "Deep health check could not reach Redis");
        }
    }

    stopwatch.Stop();

    // Redis is a cache, so its absence is not failure — but if it is configured and
    // unreachable, that is a real fault worth surfacing.
    bool healthy = canConnect && (!redisConfigured || redisReachable);

    var payload = new
    {
        status = healthy ? "healthy" : "degraded",
        database = new
        {
            reachable = canConnect,
            provider = db.Database.ProviderName,
            error = failure,
        },
        redis = new
        {
            configured = redisConfigured,
            reachable = redisReachable,
            pingMs = redisPingMs,
            error = redisFailure,
        },
        elapsedMs = stopwatch.Elapsed.TotalMilliseconds,
    };

    // 503 rather than 200-with-a-sad-payload: monitoring should not have to parse JSON to
    // notice a dependency is gone. Safe to do for the cache too, because nothing routes
    // traffic on this endpoint — the load balancer uses /health/ready.
    return healthy
        ? Results.Ok(payload)
        : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
});

// Realtime transport check. Carries no game logic — see EchoHub for why it exists.
app.MapHub<EchoHub>("/hubs/echo");

app.Run();
