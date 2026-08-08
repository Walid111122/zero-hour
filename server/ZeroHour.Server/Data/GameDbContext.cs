using Microsoft.EntityFrameworkCore;

namespace ZeroHour.Server.Data;

/// <summary>
/// Truth lives in Postgres (`15`). SQLite is a local-development convenience only —
/// it must never be the thing a release is validated against, because the two differ
/// in exactly the places that matter here: jsonb, uuid, and case-sensitive collation.
/// </summary>
public class GameDbContext : DbContext
{
    protected GameDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();

    public DbSet<PlayerState> PlayerStates => Set<PlayerState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // `15 §1` mandates snake_case. EF's default is PascalCase, which would work but
        // would make every hand-written Dapper query on the hot read paths (`15` preamble)
        // disagree with the migrations. One convention, applied centrally.
        bool isNpgsql = Database.IsNpgsql();

        modelBuilder.Entity<Player>(entity =>
        {
            entity.ToTable("players");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeviceId).HasColumnName("device_id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.AuthProvider).HasColumnName("auth_provider").IsRequired();
            entity.Property(e => e.DisplayName).HasColumnName("display_name").IsRequired();
            entity.Property(e => e.AgeDeclared).HasColumnName("age_declared");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.BannedUntil).HasColumnName("banned_until");
            entity.Property(e => e.BanReason).HasColumnName("ban_reason");
            entity.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            entity.HasIndex(e => e.DeviceId).HasDatabaseName("ix_players_device");

            // Unique but nullable: device-auth accounts have no email, and several NULLs
            // must be allowed to coexist. Postgres and SQLite both permit that; SQL Server
            // would not, which is worth knowing if this ever moves.
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("ux_players_email");
        });

        modelBuilder.Entity<PlayerState>(entity =>
        {
            entity.ToTable("player_states");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PlayerId).HasColumnName("player_id");
            entity.Property(e => e.StateId).HasColumnName("state_id");
            entity.Property(e => e.HqLevel).HasColumnName("hq_level").HasDefaultValue(1);
            entity.Property(e => e.Power).HasColumnName("power").HasDefaultValue(0L);
            entity.Property(e => e.TileX).HasColumnName("tile_x");
            entity.Property(e => e.TileY).HasColumnName("tile_y");
            entity.Property(e => e.AllianceId).HasColumnName("alliance_id");
            entity.Property(e => e.VipLevel).HasColumnName("vip_level").HasDefaultValue(0);
            entity.Property(e => e.VipPoints).HasColumnName("vip_points").HasDefaultValue(0L);
            entity.Property(e => e.ShieldUntil).HasColumnName("shield_until");
            entity.Property(e => e.Stamina).HasColumnName("stamina").HasDefaultValue(100);
            entity.Property(e => e.LastResolvedAt).HasColumnName("last_resolved_at");
            entity.Property(e => e.ConfigVersion).HasColumnName("config_version");

            // jsonb on Postgres buys indexable, validated JSON. SQLite has no such type
            // and would silently accept the column name while storing plain text, so the
            // difference is made explicit here rather than discovered in a query later.
            entity.Property(e => e.Resources)
                .HasColumnName("resources")
                .HasColumnType(isNpgsql ? "jsonb" : "TEXT")
                .IsRequired();

            entity.HasOne(e => e.Player)
                .WithMany(p => p.States)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.PlayerId, e.StateId })
                .IsUnique()
                .HasDatabaseName("ux_ps_player_state");

            entity.HasIndex(e => e.AllianceId).HasDatabaseName("ix_ps_alliance");

            entity.HasIndex(e => new { e.StateId, e.TileX, e.TileY })
                .HasDatabaseName("ix_ps_tile");

            // Descending on power: every leaderboard read is "top N by power", and an
            // ascending index makes the server sort the whole shard to answer that.
            entity.HasIndex(e => new { e.StateId, e.Power })
                .IsDescending(false, true)
                .HasDatabaseName("ix_ps_power");
        });
    }
}
