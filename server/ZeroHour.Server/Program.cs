using System.Diagnostics;
using System.Reflection;
using ZeroHour.Sim;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

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

app.Run();
