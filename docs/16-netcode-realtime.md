# 16 — Real-Time Netcode (★ Arena)

> The hardest engineering in the project. Everything else is CRUD with timers.
> Phase 5. Read `11-FEATURE-arena-rooms.md` first.

---

## 1. Model choice

| Model | Verdict |
|---|---|
| Lockstep deterministic | ❌ One laggy player stalls everyone. Unusable on mobile |
| Full client authority | ❌ Trivially cheatable |
| Rollback netcode | ❌ Correct for fighting games; overkill and CPU-expensive for 40 players |
| **Server-authoritative + client prediction + interpolation** | ✅ **Chosen** |

**Why:** it is the standard model for this problem, it tolerates variable mobile latency, it is cheat-resistant by construction, and 20 Hz squad-level movement doesn't need anything more sophisticated.

---

## 2. The loop

```
CLIENT (60 fps render)                SERVER (20 Hz fixed tick)
──────────────────────                ─────────────────────────
player taps "move here"
  → create Intent{seq, tick, cmd}
  → apply LOCALLY (prediction)
  → send Intent  ────────────────▶    buffer intents by arrival
                                      on tick T:
                                        - apply all intents ≤ T
                                        - ArenaSim.Step(state, dt)
                                        - build delta snapshot
  interpolate remote entities   ◀────  broadcast at 10 Hz
  reconcile own squad
    if |predicted - authoritative| > ε
       → smooth-correct over 200 ms
```

### Key rates
| | Value | Why |
|---|---|---|
| Server tick | **20 Hz** (50 ms) | Enough for squad movement; ¼ the CPU of 60 Hz |
| Snapshot send | **10 Hz** | Halves bandwidth; interpolation hides the gap |
| Client render | 60 fps | Interpolated, decoupled from network rate |
| Interpolation delay | **100 ms** (2 snapshots) | Absorbs jitter; the standard trade |
| Input send | On action, plus 10 Hz heartbeat | Squad games are low-input |

---

## 3. Messages

### Client → Server (intents only)
```csharp
[MessagePackObject]
public struct ArenaIntent {
    [Key(0)] public uint  Seq;         // client sequence, for reconciliation
    [Key(1)] public uint  ClientTick;
    [Key(2)] public byte  Command;     // Move|Attack|Formation|Skill|Retreat
    [Key(3)] public short TargetX;     // fixed-point world coords
    [Key(4)] public short TargetY;
    [Key(5)] public int   TargetEntity;
    [Key(6)] public byte  Param;       // skill index / formation id
}
```
~16 bytes. **The client never sends positions, damage, or results — only what the player asked for.**

### Server → Client (delta snapshots)
```csharp
public struct ArenaSnapshot {
    public uint  Tick;
    public uint  AckSeq;               // last intent processed, per client
    public EntityDelta[] Changed;      // only entities that changed
    public GameEvent[]   Events;       // deaths, skill casts, objectives
}
public struct EntityDelta {            // 12 bytes
    public ushort Id;
    public short  X, Y;                // fixed-point
    public ushort Hp;
    public byte   Count;               // troops remaining
    public byte   Flags;               // state bits
}
```

**Bandwidth:**
```
20 entities changed × 12 B = 240 B + header ≈ 300 B
10 Hz → 3 KB/s down per client
Uplink ≈ 0.2 KB/s
20v20 → 40 clients × 3 KB/s ≈ 120 KB/s ≈ 1 Mbps per match
```
Ten concurrent 20v20 matches ≈ 10 Mbps. Comfortable on the free VM's 10 TB/month.

---

## 4. Prediction & reconciliation

```csharp
// Client keeps a ring of unacked intents
void OnSnapshot(ArenaSnapshot snap) {
    pending.RemoveWhere(i => i.Seq <= snap.AckSeq);

    var authoritative = snap.GetOwnSquad();
    if (Fixed.Distance(predicted.Pos, authoritative.Pos) > Threshold) {
        // Rewind to authoritative, replay unacked intents
        var s = authoritative;
        foreach (var i in pending) ArenaSim.ApplyIntent(ref s, i);
        // Never snap — blend over 200 ms
        corrector.Begin(predicted.Pos, s.Pos, 0.2f);
    }
}
```

**Rules:**
- Only the **player's own squad** is predicted. Remote entities are purely interpolated.
- Corrections are always **smoothed**, never snapped. A visible teleport reads as a bug even when the netcode is right.
- Threshold ≈ 0.5 world units. Below that, ignore the difference entirely.
- **Damage and deaths are never predicted.** They come from the server only. Predicting a kill that didn't happen is far worse than a 100 ms delay.

---

## 5. Server match host

```csharp
public sealed class ArenaMatchHost {
    readonly ArenaState _state;
    readonly Channel<ArenaIntent> _inbox;
    const int TickMs = 50;

    public async Task RunAsync(CancellationToken ct) {
        var next = _clock.NowMs();
        while (!ct.IsCancellationRequested && !_state.IsOver) {
            DrainIntents();                            // validate + enqueue
            ArenaSim.Step(ref _state, TickMs);         // shared/Sim, fixed-point
            if (_state.Tick % 2 == 0) await Broadcast();  // 10 Hz
            _recorder.Record(_state.Tick, _appliedIntents);

            next += TickMs;
            var delay = next - _clock.NowMs();
            if (delay > 0) await Task.Delay((int)delay, ct);
            else _metrics.TickOverrun++;               // alarm if sustained
        }
        await FinalizeAsync();                          // results, ELO, replay
    }
}
```

- One host object per match, scheduled on the thread pool. No dedicated thread per match.
- `ArenaSim.Step` is pure fixed-point maths — cheap. A 20v20 tick is well under 1 ms.
- CPU estimate: 10 concurrent 20v20 ≈ 200 ticks/s total ≈ well within one core.
- **Tick overrun is the canary.** If it rises, cap concurrent matches and queue.

---

## 6. Intent validation (server-side, always)

Every intent is checked before it is applied:

| Check | Rejection reason |
|---|---|
| Player is in this match | Spoofed match id |
| Squad belongs to the player | Controlling someone else's squad |
| Target is inside map bounds | Out-of-bounds teleport |
| Skill is on the squad and off cooldown | Cooldown bypass |
| Intent rate ≤ 20/s | Input flooding |
| `ClientTick` within ±2 s of server tick | Timestamp manipulation |
| Retreat not already used | Ability spam |

Rejected intents are dropped silently and counted. A player exceeding a rejection threshold is flagged for review (`20`).

---

## 7. Lag compensation

Deliberately **minimal**. Squad-level tactical combat with ~1 s engagement windows does not need per-shot rewind:

- **No rewind hit detection.** Damage happens on the server's current tick.
- **Generous engagement ranges** — a small position discrepancy doesn't change whether units are fighting.
- **Orders are timestamped and applied on arrival**, with intents older than 200 ms clamped to 200 ms of lateness. High-ping players are slightly disadvantaged, which is honest and much simpler than the alternative.
- Ping is displayed so players understand what they're experiencing.

**Design consequence:** because combat is squad-level and continuous rather than shot-based, 150 ms of latency is barely noticeable. This is a large part of why arena combat was designed as squad-tactical rather than unit-precise.

---

## 8. Connection management

| Event | Handling |
|---|---|
| Join | Full state snapshot, then deltas |
| Packet loss | Deltas are cumulative from the last acked tick; a missed snapshot self-heals on the next |
| Brief disconnect (< 30 s) | Slot held, defensive AI, full resync on reconnect |
| Long disconnect | AI plays out the match |
| Reconnect | Full snapshot + resume deltas |
| Server crash | Match voided, no ELO/War Point change, entries refunded |

**Transport:** SignalR over WebSocket with MessagePack. Not raw UDP.
- Reconnection, backpressure, and grouping are already solved
- WebSocket works everywhere, including WebGL
- At 10 Hz with tiny payloads, TCP head-of-line blocking is not a practical problem
- If it ever becomes one, the transport is behind an interface and can be swapped

---

## 9. Replays

```csharp
Replay {
  MatchId, Seed, TickRate,
  InitialState,          // full serialized ArenaState
  InputLog: [(tick, playerId, intent)...]
}
```
Deterministic `ArenaSim` means the input log reproduces the match exactly. A few KB per match.

Used for: in-app viewing, alliance sharing, admin review, and **server-side re-simulation of any disputed result**.

---

## 10. Testing

| Test | Method |
|---|---|
| Determinism | 1,000 fixtures → identical final state hash on Windows x64, Android ARM64, and Linux ARM server |
| Latency tolerance | Simulated 50/150/300/500 ms RTT |
| Packet loss | 1% / 5% / 10% loss |
| Jitter | ±100 ms |
| Load | 10 concurrent 20v20, 10 min, watch tick overruns |
| Reconnect | Kill and restore the connection at 20 points during a match |
| Cheat attempts | Malformed, out-of-range, and replayed intents |
| Bandwidth | Measured per client, must stay < 5 KB/s |

**Network conditions are simulated with `clumsy` (Windows) or `tc netem` (Linux) in CI.** Testing only on a good connection is how shipped netcode fails.

---

## 11. Build order (critical)

```
1. ArenaSim in shared/Sim, unit-tested, no networking at all
2. Local single-player arena — verify the sim feels good
3. Server host with 20 Hz loop, one client, no prediction
4. Add snapshots + interpolation
5. Add prediction + reconciliation
6. 1v1 over the real network  ← THE VERTICAL SLICE
7. Scale: 3v3 → 5v5 → 10v10 → 20v20, load testing at each step
8. Reconnect, AI takeover, replays
```

**Do not skip step 2.** If the combat isn't fun offline, networking will not make it fun — it will only make the un-fun harder to diagnose.

---

## Next
- `11-FEATURE-arena-rooms.md` — the design this serves
- `20-security-anticheat.md`
- `23-qa-testing.md`
