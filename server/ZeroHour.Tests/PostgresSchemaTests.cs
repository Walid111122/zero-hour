using Microsoft.EntityFrameworkCore;
using Npgsql;
using ZeroHour.Server.Data;

namespace ZeroHour.Tests;

/// <summary>
/// Skips instead of failing when no Postgres is configured, so a developer without Docker
/// still gets a green local run — while CI, which does provide one, actually executes them.
/// </summary>
/// <remarks>
/// A skip is a compromise worth naming: it means "not verified here", and a suite that is
/// green because everything skipped looks identical to one that genuinely passed. CI runs
/// these against a real Postgres service container, so the coverage exists somewhere it
/// cannot be quietly lost.
/// </remarks>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(PostgresFixture.ConnectionString))
        {
            Skip = "No ConnectionStrings__Postgres configured; skipping Postgres-backed test.";
        }
    }
}

/// <summary>Migrates a real database once for the whole class.</summary>
public sealed class PostgresFixture : IDisposable
{
    public static string? ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");

    public PostgresGameDbContext? Db { get; private set; }

    public PostgresFixture()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            return;
        }

        DbContextOptions<PostgresGameDbContext> options =
            new DbContextOptionsBuilder<PostgresGameDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

        Db = new PostgresGameDbContext(options);

        // Drop first so a re-run against a warm database tests the migration rather than
        // whatever schema a previous run happened to leave behind.
        Db.Database.EnsureDeleted();
        Db.Database.Migrate();
    }

    public void Dispose() => Db?.Dispose();
}

/// <summary>
/// The SQLite tests prove the model's shape. These prove the things only the real provider
/// can answer — jsonb is genuinely jsonb and not text wearing its name, identity columns
/// generate keys, and the descending index that every leaderboard read depends on exists.
/// </summary>
public sealed class PostgresSchemaTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public PostgresSchemaTests(PostgresFixture fixture)
    {
        _fixture = fixture;

        // Each test starts from empty. Truncate rather than delete so identity counters
        // reset too, keeping assertions about generated ids stable.
        _fixture.Db?.Database.ExecuteSqlRaw(
            "TRUNCATE player_states, players RESTART IDENTITY CASCADE;");
    }

    [PostgresFact]
    public void Resources_is_really_jsonb()
    {
        // `15 §1` calls for jsonb, and the model asks for it — but only the real provider
        // can confirm the column was created that way. On SQLite the same mapping silently
        // becomes TEXT, so this is the assertion that keeps the two from drifting.
        string? type = ScalarString(
            """
            SELECT data_type FROM information_schema.columns
            WHERE table_name = 'player_states' AND column_name = 'resources';
            """);

        Assert.Equal("jsonb", type);
    }

    [PostgresFact]
    public void Player_id_is_a_real_uuid_column()
    {
        string? type = ScalarString(
            """
            SELECT data_type FROM information_schema.columns
            WHERE table_name = 'players' AND column_name = 'id';
            """);

        Assert.Equal("uuid", type);
    }

    [PostgresFact]
    public void Timestamps_are_bigint_not_timestamptz()
    {
        // `15 §1`: the shared Sim takes time as a long. A well-meaning change to timestamptz
        // would compile, migrate, and then quietly reintroduce timezone bugs into the one
        // place that must stay identical on client and server.
        string? type = ScalarString(
            """
            SELECT data_type FROM information_schema.columns
            WHERE table_name = 'players' AND column_name = 'created_at';
            """);

        Assert.Equal("bigint", type);
    }

    [PostgresFact]
    public void Identity_generates_player_state_ids()
    {
        GameDbContext db = Require();

        Player player = NewPlayer();
        db.Players.Add(player);

        PlayerState state = NewState(player.Id);
        db.PlayerStates.Add(state);
        db.SaveChanges();

        Assert.True(state.Id > 0);
    }

    [PostgresFact]
    public void Jsonb_can_be_queried_by_key()
    {
        GameDbContext db = Require();

        Player player = NewPlayer();
        db.Players.Add(player);
        db.PlayerStates.Add(NewState(player.Id));
        db.SaveChanges();

        // The point of jsonb over text: the database can read inside the blob. If this
        // column were ever created as text, this query fails rather than silently degrading.
        string? food = ScalarString(
            "SELECT resources ->> 'food' FROM player_states LIMIT 1;");

        Assert.Equal("100", food);
    }

    [PostgresFact]
    public void Power_index_is_descending()
    {
        // Leaderboards read "top N by power". An ascending index would still answer, by
        // sorting the whole shard first — correct, and progressively slower as it fills.
        string? definition = ScalarString(
            "SELECT indexdef FROM pg_indexes WHERE indexname = 'ix_ps_power';");

        Assert.NotNull(definition);
        Assert.Contains("DESC", definition);
    }

    [PostgresFact]
    public void A_player_cannot_join_the_same_state_twice()
    {
        GameDbContext db = Require();

        Player player = NewPlayer();
        db.Players.Add(player);
        db.PlayerStates.Add(NewState(player.Id));
        db.SaveChanges();

        db.PlayerStates.Add(NewState(player.Id));

        DbUpdateException error = Assert.Throws<DbUpdateException>(() => db.SaveChanges());
        Assert.IsType<PostgresException>(error.InnerException);
    }

    [PostgresFact]
    public void Two_device_accounts_can_both_have_no_email()
    {
        GameDbContext db = Require();

        Player first = NewPlayer();
        first.Email = null;

        Player second = NewPlayer();
        second.Email = null;

        db.Players.AddRange(first, second);
        db.SaveChanges();

        Assert.Equal(2, db.Players.Count());
    }

    private GameDbContext Require() =>
        _fixture.Db ?? throw new InvalidOperationException("Postgres fixture not initialised.");

    private string? ScalarString(string sql)
    {
        using NpgsqlConnection connection = new(PostgresFixture.ConnectionString);
        connection.Open();

        using NpgsqlCommand command = new(sql, connection);
        object? value = command.ExecuteScalar();

        return value as string;
    }

    private static Player NewPlayer() => new()
    {
        Id = Guid.NewGuid(),
        DeviceId = "device-1",
        AuthProvider = "device",
        DisplayName = "Commander",
        CreatedAt = 1_699_000_000_000L,
    };

    private static PlayerState NewState(Guid playerId) => new()
    {
        PlayerId = playerId,
        StateId = 1,
        LastResolvedAt = 1_699_000_000_000L,
        Resources = """{"food":100,"iron":50}""",
        ConfigVersion = 1,
    };
}
