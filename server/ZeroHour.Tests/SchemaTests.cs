using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ZeroHour.Server.Data;

namespace ZeroHour.Tests;

/// <summary>
/// Applies the real migration to a throwaway database and checks the constraints actually
/// bite. `EnsureCreated` is deliberately avoided: it builds tables from the model and would
/// pass even if the migration were broken, which is the exact failure this guards against.
/// </summary>
/// <remarks>
/// These run on SQLite, so they verify the migration applies and the model's keys, defaults
/// and relationships behave. They cannot verify Postgres-specific behaviour — jsonb operators
/// and identity semantics need the real provider, which arrives with Testcontainers once
/// Docker is available.
/// </remarks>
public sealed class SchemaTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteGameDbContext _db;

    public SchemaTests()
    {
        // A shared in-memory database lives exactly as long as its connection, so each test
        // class gets a clean schema with no files to clean up and no cross-test bleed.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        DbContextOptions<SqliteGameDbContext> options =
            new DbContextOptionsBuilder<SqliteGameDbContext>()
                .UseSqlite(_connection)
                .Options;

        _db = new SqliteGameDbContext(options);
        _db.Database.Migrate();
    }

    [Fact]
    public void Migration_creates_the_expected_tables()
    {
        List<string> tables = TableNames();

        Assert.Contains("players", tables);
        Assert.Contains("player_states", tables);
    }

    [Fact]
    public void Tables_and_columns_use_snake_case()
    {
        // `15 §1`. Worth asserting because EF's default is PascalCase: if the explicit
        // mapping is ever dropped, the migration still succeeds and every hand-written
        // Dapper query on the hot read paths starts failing instead.
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = "SELECT name FROM pragma_table_info('player_states');";

        List<string> columns = new();
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            columns.Add(reader.GetString(0));
        }

        Assert.Contains("player_id", columns);
        Assert.Contains("last_resolved_at", columns);
        Assert.Contains("config_version", columns);
        Assert.DoesNotContain("PlayerId", columns);
    }

    [Fact]
    public void Player_and_state_round_trip()
    {
        Player player = NewPlayer();
        _db.Players.Add(player);
        _db.PlayerStates.Add(NewState(player.Id));
        _db.SaveChanges();

        _db.ChangeTracker.Clear();

        Player loaded = _db.Players.Include(p => p.States).Single();

        Assert.Equal("Commander", loaded.DisplayName);
        Assert.Single(loaded.States);
        Assert.Equal(1_699_000_000_000L, loaded.CreatedAt);

        // The resources blob survives verbatim. It is opaque to the database on SQLite,
        // so a silent mangling here would only show up much later as bad player state.
        Assert.Equal("{\"food\":100,\"iron\":50}", loaded.States.First().Resources);
    }

    [Fact]
    public void Defaults_from_the_schema_are_applied()
    {
        Player player = NewPlayer();
        _db.Players.Add(player);

        // Left unset so the database supplies them, rather than the C# property initialisers.
        PlayerState state = new()
        {
            PlayerId = player.Id,
            StateId = 1,
            LastResolvedAt = 1_699_000_000_000L,
            Resources = "{}",
            ConfigVersion = 1,
        };

        _db.PlayerStates.Add(state);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();

        PlayerState loaded = _db.PlayerStates.Single();

        Assert.Equal(1, loaded.HqLevel);
        Assert.Equal(100, loaded.Stamina);
        Assert.Equal(0, loaded.VipLevel);
        Assert.Equal(0L, loaded.Power);
    }

    [Fact]
    public void A_player_cannot_join_the_same_state_twice()
    {
        Player player = NewPlayer();
        _db.Players.Add(player);
        _db.PlayerStates.Add(NewState(player.Id));
        _db.SaveChanges();

        _db.PlayerStates.Add(NewState(player.Id));

        // Enforced by the database, not by application code. A duplicate row here would
        // mean two divergent bases for one account in one shard — corrupt in a way that is
        // very hard to unpick after the fact.
        Assert.Throws<DbUpdateException>(() => _db.SaveChanges());
    }

    [Fact]
    public void A_state_cannot_reference_a_player_that_does_not_exist()
    {
        _db.PlayerStates.Add(NewState(Guid.NewGuid()));

        Assert.Throws<DbUpdateException>(() => _db.SaveChanges());
    }

    [Fact]
    public void Two_device_accounts_can_both_have_no_email()
    {
        // The unique index on email must still allow multiple NULLs, or the second
        // device-auth signup in a shard fails. Easy to get wrong, and it would look like
        // an unrelated registration bug.
        Player first = NewPlayer();
        first.Email = null;

        Player second = NewPlayer();
        second.Email = null;
        second.DeviceId = "device-2";

        _db.Players.AddRange(first, second);

        _db.SaveChanges();

        Assert.Equal(2, _db.Players.Count());
    }

    [Fact]
    public void Two_accounts_cannot_share_an_email()
    {
        Player first = NewPlayer();
        first.Email = "one@example.test";

        Player second = NewPlayer();
        second.Email = "one@example.test";

        _db.Players.AddRange(first, second);

        Assert.Throws<DbUpdateException>(() => _db.SaveChanges());
    }

    [Fact]
    public void Deleting_a_player_with_states_is_refused()
    {
        Player player = NewPlayer();
        _db.Players.Add(player);
        _db.PlayerStates.Add(NewState(player.Id));
        _db.SaveChanges();

        // DeleteBehavior.Restrict, matching the soft-delete rule in `15 §1`: player-visible
        // rows are retired with `deleted_at`, never removed, so account recovery and
        // chargeback investigations still have something to read.
        //
        // Remove() is inside the assertion because that is where it actually throws. With
        // the dependent rows tracked, EF applies the delete behaviour during change-tracker
        // fixup, before any SQL is generated — so SaveChanges is never reached. Asserting on
        // SaveChanges alone would let the exception escape and fail the test even though the
        // deletion was correctly refused.
        Assert.Throws<InvalidOperationException>(() =>
        {
            _db.Players.Remove(player);
            _db.SaveChanges();
        });
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
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
        Resources = "{\"food\":100,\"iron\":50}",
        ConfigVersion = 1,
    };

    private List<string> TableNames()
    {
        using SqliteCommand command = _connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";

        List<string> names = new();
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }
}
