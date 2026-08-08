# 14 — Technical Architecture

> Server-authoritative, data-driven, deterministic, and free to host until revenue arrives.

---

## 1. Stack

| Layer | Choice | Cost |
|---|---|---|
| Client | **Unity 6000.5.6f1**, URP, C# | Free (Personal, < $200k revenue) |
| Shared sim | **.NET 10** class library (`netstandard2.1` target for Unity compat) | Free |
| Server | **ASP.NET Core 10** + SignalR | Free |
| Database | **PostgreSQL 16** | Free |
| Cache / queue / pubsub | **Redis 7** | Free |
| Voice ★ | **LiveKit** SFU + coturn | Free (self-hosted) |
| Translate | **LibreTranslate** | Free (self-hosted) |
| Host | **Oracle Cloud Always Free** — 4 ARM cores, 24 GB RAM, 200 GB, 10 TB/mo egress | **$0** |
| CI | **GitHub Actions** (2000 min/mo free) | Free |
| Analytics | **PostHog** self-hosted or free cloud tier | Free |
| Crash reporting | **Sentry** free tier | Free |
| Container orchestration | **Docker Compose** | Free |

**Total infrastructure cost: $0** until roughly 5,000–10,000 DAU. The only unavoidable spend before launch is the **$25 one-time Google Play registration**.

---

## 2. Three-project layout

```
ZeroHour.sln
├── client/                      Unity project
│   └── Assets/_Project/Scripts/
│       ├── ZeroHour.Core       (services, DI, save, config)
│       ├── ZeroHour.Runner     (Phase 1 runner views)
│       ├── ZeroHour.Base       (base layer views)
│       ├── ZeroHour.World      (world map views)
│       ├── ZeroHour.Arena  ★   (arena views + netcode client)
│       ├── ZeroHour.Voice  ★   (IVoiceService + implementations)
│       ├── ZeroHour.UI         (all UI, red-dot tree, juice)
│       └── ZeroHour.Net        (transport, DTOs, reconnect)
│
├── shared/
│   └── ZeroHour.Sim/            ★ THE MOST IMPORTANT PROJECT
│       ├── Fixed/               Q32.32 fixed-point maths
│       ├── Rng/                 xoshiro256** deterministic PRNG
│       ├── Model/               pure state structs
│       ├── Rules/               all game formulas
│       ├── Combat/              BattleResolver + ArenaSim
│       ├── Runner/              RunnerSim
│       ├── Economy/             production, costs, timers
│       └── Generated/           codegen'd balance tables
│
└── server/
    ├── ZeroHour.Server/         ASP.NET Core host
    │   ├── Api/                 REST endpoints
    │   ├── Hubs/                SignalR (world, chat, ★ arena)
    │   ├── Services/            game services
    │   ├── Persistence/         EF Core + Dapper
    │   └── Jobs/                scheduled work
    ├── ZeroHour.Admin/          admin panel (Blazor)
    └── ZeroHour.Tests/          server + sim tests
```

### The shared/Sim rule
**`ZeroHour.Sim` is referenced by both the client and the server as the same compiled assembly.**

This is the single most important architectural decision in the project. Consequences:
- The client can predict any outcome exactly, because it runs the server's code
- The server can validate any client claim by re-running it
- Balance changes apply to both sides automatically
- There is no possible "the client thinks X, the server thinks Y" bug class

The Unity side references it via an assembly definition pointing at the built DLL, produced by `dotnet build` and copied by `tools/scripts/build-sim.ps1`.

### Sim constraints (enforced by an analyzer in CI)
```
❌ float, double, decimal          → use Fixed
❌ UnityEngine.*                   → server has no Unity
❌ DateTime.Now / UtcNow           → time is a parameter
❌ System.Random                   → use DeterministicRng
❌ Dictionary enumeration in sim   → use sorted collections
❌ LINQ in hot paths               → allocation + ordering risk
❌ any I/O                         → pure functions only
```

---

## 3. Server architecture

```
                    ┌──────────────────────────┐
   Unity clients ──▶│  Nginx (TLS, rate limit) │
                    └────────────┬─────────────┘
                                 │
                ┌────────────────┴────────────────┐
                │      ZeroHour.Server            │
                │  ┌──────────┐  ┌─────────────┐  │
                │  │ REST API │  │  SignalR    │  │
                │  │  (state, │  │  hubs       │  │
                │  │  actions)│  │ world/chat/ │  │
                │  └──────────┘  │  ★ arena    │  │
                │                └─────────────┘  │
                │  ┌────────────────────────────┐ │
                │  │ Services                   │ │
                │  │ Player · Base · World      │ │
                │  │ Alliance · Event · Arena ★ │ │
                │  │ Voice ★ · Purchase · Anti  │ │
                │  └────────────────────────────┘ │
                │  ┌────────────────────────────┐ │
                │  │ shared/ZeroHour.Sim        │ │
                │  └────────────────────────────┘ │
                └───────┬───────────────┬─────────┘
                        │               │
                 ┌──────▼─────┐  ┌──────▼──────┐
                 │ PostgreSQL │  │   Redis     │
                 │  (truth)   │  │ cache/queue │
                 └────────────┘  └─────────────┘
                        │
        ┌───────────────┼────────────────┐
   ┌────▼─────┐  ┌──────▼──────┐  ┌──────▼────────┐
   │ LiveKit ★│  │LibreTranslate│  │ PostHog       │
   └──────────┘  └─────────────┘  └───────────────┘
```

All services run as Docker Compose containers on the single free VM.

---

## 4. Lazy state evaluation — the scaling trick

**Never tick the world.** State is resolved when read.

```csharp
public PlayerState Resolve(PlayerState s, long nowMs)
{
    long dt = nowMs - s.LastResolvedAtMs;
    if (dt <= 0) return s;

    // Production accrues, clamped to capacity
    s.Resources = Economy.Accrue(s.Resources, s.Rates, dt, s.Caps);

    // Timers: completion is a comparison, not a countdown
    foreach (var job in s.Jobs.Sorted())
        if (nowMs >= job.CompletesAtMs) Complete(ref s, job);

    s.LastResolvedAtMs = nowMs;
    return s;
}
```

| Approach | 40,000 players |
|---|---|
| Tick every base every second | 40,000 writes/sec — needs a cluster |
| **Resolve on read** | Cost ∝ *active* players — a single free VM |

The same principle covers marches (`07 §3.2`), research, training, and healing. Arrival-time events go into a **Redis sorted set** keyed by timestamp; one lightweight worker pops due entries. Nothing polls.

---

## 5. Request flow (an example)

```
Client: POST /api/base/upgrade { buildingId: 3 }
   ↓ Nginx: TLS, rate limit (60 req/min/player)
   ↓ Auth middleware: JWT → playerId
   ↓ BaseService.UpgradeBuilding(playerId, buildingId)
       1. Load PlayerState (Redis cache → Postgres on miss)
       2. Sim.Resolve(state, now)                    ← lazy accrual
       3. Sim.Rules.CanUpgrade(state, buildingId)    ← authoritative check
       4. Sim.Rules.ApplyUpgrade(ref state, now)     ← deduct + schedule
       5. Persist (Postgres) + cache (Redis)
       6. Schedule completion in the Redis sorted set
       7. Emit analytics + event-scoring triggers
   ↓ Response: { state delta, jobId, completesAt }
Client: applies the delta; its own Sim already predicted the same result
```

**Every mutation follows this shape.** No endpoint accepts a client-computed value — only intents.

---

## 6. Data flow & authority

| Data | Authority | Client role |
|---|---|---|
| Resources, buildings, troops, heroes | **Server** | Predict, display |
| Timers | **Server** (server time only) | Display countdown |
| Battle results | **Server** | Render replay |
| Runner stage result | **Server re-simulates** | Play, submit input log |
| ★ Arena match state | **Server** at 20 Hz | Predict own squad, interpolate others |
| ★ Voice room membership | **Server** (mints JWT) | Connect with token |
| Purchases | **Server** (receipt validation) | Initiate only |
| UI state, settings, camera | Client | Local |

---

## 7. Server-driven UI (the live-ops enabler)

Required so events launch without app updates (`09 §1`).

```json
{
  "screen": "event",
  "template": "milestone_ladder",
  "header": { "title": "@loc:event.arms_race", "banner": "cdn://banners/ar.png" },
  "sections": [
    { "type": "timer", "endsAt": 1735689600 },
    { "type": "progress", "current": 4200, "milestones": [1000, 5000, 20000] },
    { "type": "reward_grid", "items": [ ... ] },
    { "type": "leaderboard", "scope": "state", "top": 20 }
  ],
  "actions": [ { "type": "claim", "milestoneId": 2 } ]
}
```

The client ships **~8 templates** and a set of generic section renderers. Any new event is a JSON document plus localisation strings — no client build.

**Constraint:** server-driven UI is for *events and offers only*. Core gameplay screens are native, because a fully server-driven UI system is a second engine and we are not building one.

---

## 8. Configuration & hot reload

```
tools/balance/*.csv
   ↓ codegen (tools/scripts/gen-balance.ps1)
shared/ZeroHour.Sim/Generated/*.cs      (compiled, strongly typed)
   ↓
Server loads at boot + on SIGHUP / admin "reload config"
Client receives configVersion in every response
   → on mismatch, fetches /api/config and swaps tables at a safe boundary
```

Balance changes are a server reload plus a small client config fetch. No store release.

---

## 9. Scaling path (when free stops being enough)

| DAU | Action | Cost |
|---|---|---|
| < 5k | Single free VM | $0 |
| 5–20k | Split DB to its own VM; second app VM behind Nginx | ~$40/mo |
| 20–50k | Managed Postgres, Redis cluster, 3 app nodes, dedicated ★ arena hosts | ~$300/mo |
| 50k+ | Shard by state; regional deployments; CDN for assets | ~$1,500/mo |

The architecture supports this without rewrites because:
- App servers are **stateless** (all state in Postgres/Redis)
- The world is **already sharded by state** — a state is a natural partition
- ★ Arena matches are independent processes, trivially horizontally scalable
- Voice is a separate service with its own scaling story

---

## 10. Key architectural decisions (and the reasoning)

| Decision | Why |
|---|---|
| Server-authoritative everything | Anti-cheat, and it's cheaper than fixing exploits later |
| Shared `Sim` assembly | Eliminates the entire client/server divergence bug class |
| Fixed-point maths | Floats diverge across ARM/x86; determinism is non-negotiable for ★ arena and runner validation |
| Lazy evaluation | Turns an O(players) cost into O(active players) |
| Data-driven balance | Retune without a release |
| Server-driven event UI | Live-ops without a release |
| Monolith, not microservices | One developer. A monolith you can reason about beats a distributed system you can't |
| Docker Compose, not Kubernetes | Same reason |
| Postgres, not NoSQL | The data is highly relational (alliances, members, marches). Postgres also gives us JSONB where we want flexibility |
| SignalR, not raw WebSockets | Reconnection, backpressure, and grouping are already solved |
| ★ Arena at 20 Hz | Enough for squad-level tactics; a quarter the CPU of 60 Hz |

---

## Next
- `15-data-schema.md`
- `16-netcode-realtime.md`
- `24-devops-deployment.md`
