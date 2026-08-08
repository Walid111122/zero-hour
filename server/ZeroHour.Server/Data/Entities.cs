namespace ZeroHour.Server.Data;

/// <summary>An account. One per human, independent of which state they play in.</summary>
/// <remarks>
/// Every timestamp here is epoch milliseconds UTC as a <see cref="long"/>, never a
/// <c>DateTime</c> (`15 §1`). The shared Sim takes time as a <c>long</c>, so storing
/// exactly what the sim consumes removes a class of timezone and precision bugs at
/// the boundary rather than trying to convert correctly at every call site.
/// </remarks>
public sealed class Player
{
    /// <summary>Uuid rather than an identity bigint, so it can be issued client-side
    /// and is not guessable by enumeration (`15 §1`).</summary>
    public Guid Id { get; set; }

    public string? DeviceId { get; set; }

    public string? Email { get; set; }

    /// <summary>device | google | email</summary>
    public string AuthProvider { get; set; } = "device";

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Self-declared age, gating voice features (`10 §6.1`). Nullable because
    /// it is unknown until asked, and "unknown" must not read as "adult".</summary>
    public short? AgeDeclared { get; set; }

    public long CreatedAt { get; set; }

    public long? LastLoginAt { get; set; }

    public long? BannedUntil { get; set; }

    public string? BanReason { get; set; }

    /// <summary>Soft delete (`15 §1`). Player-visible rows are never hard-deleted, so
    /// an account recovery or a chargeback investigation still has something to read.</summary>
    public long? DeletedAt { get; set; }

    public ICollection<PlayerState> States { get; set; } = new List<PlayerState>();
}

/// <summary>
/// A player's presence in one game state (shard). One player may hold several.
/// </summary>
public sealed class PlayerState
{
    public long Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player? Player { get; set; }

    public int StateId { get; set; }

    public int HqLevel { get; set; } = 1;

    public long Power { get; set; }

    public int? TileX { get; set; }

    public int? TileY { get; set; }

    /// <summary>No foreign key yet — the alliances table arrives in Phase 4. Declaring
    /// the constraint before the target exists would block this migration.</summary>
    public long? AllianceId { get; set; }

    public int VipLevel { get; set; }

    public long VipPoints { get; set; }

    public long? ShieldUntil { get; set; }

    public int Stamina { get; set; } = 100;

    /// <summary>
    /// Anchor for lazy evaluation: resource accrual is computed from this instant on
    /// read rather than by a ticking job, so an idle account costs nothing.
    /// </summary>
    public long LastResolvedAt { get; set; }

    /// <summary>
    /// {food, iron, coin, diamonds, valor} as jsonb. A blob because the shape shifts
    /// with balance work, and integers only — no floats anywhere near currency (`15 §1`).
    /// </summary>
    public string Resources { get; set; } = "{}";

    /// <summary>Which balance-config version last resolved this row, so a config change
    /// can be reconciled rather than silently applied to stale numbers.</summary>
    public int ConfigVersion { get; set; }
}
