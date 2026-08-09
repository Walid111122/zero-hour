using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ZeroHour.Server.Data;

// A migration is generated SQL, so it belongs to one provider. A single shared migration
// set would emit whichever dialect happened to be configured when `migrations add` ran, and
// applying that to the other provider fails at best or silently produces the wrong column
// types at worst.
//
// Two derived contexts, each with its own migrations folder and snapshot, keeps the model in
// one place while letting each provider own its own SQL. The cost is remembering to add a
// migration twice; the alternative is a migration that only works on the developer's laptop.

/// <summary>Production. Truth lives here (`15`).</summary>
public sealed class PostgresGameDbContext : GameDbContext
{
    public PostgresGameDbContext(DbContextOptions<PostgresGameDbContext> options)
        : base(options)
    {
    }
}

/// <summary>Local development only, so the loop does not require a running Postgres.</summary>
public sealed class SqliteGameDbContext : GameDbContext
{
    public SqliteGameDbContext(DbContextOptions<SqliteGameDbContext> options)
        : base(options)
    {
    }
}

// The design-time factories below exist so `dotnet ef` can construct a context without
// booting the app. Without them the tool builds the whole host, which means it needs real
// configuration and a reachable database just to scaffold a migration.

public sealed class PostgresDesignTimeFactory : IDesignTimeDbContextFactory<PostgresGameDbContext>
{
    public PostgresGameDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<PostgresGameDbContext> options = new();

        // `migrations add` never opens a connection — it only needs the provider to pick a
        // dialect, so a placeholder is enough and a real credential in source would be a leak.
        // `database update` **does** connect, though, and a hardcoded placeholder makes that
        // command permanently impossible ("No password has been provided"). So honour the same
        // environment variable the app reads, and fall back to the placeholder for scaffolding.
        string? fromEnv = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");

        options.UseNpgsql(string.IsNullOrWhiteSpace(fromEnv)
            ? "Host=localhost;Database=zerohour;Username=postgres"
            : fromEnv);

        return new PostgresGameDbContext(options.Options);
    }
}

public sealed class SqliteDesignTimeFactory : IDesignTimeDbContextFactory<SqliteGameDbContext>
{
    public SqliteGameDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<SqliteGameDbContext> options = new();
        options.UseSqlite("Data Source=zerohour.dev.db");

        return new SqliteGameDbContext(options.Options);
    }
}
