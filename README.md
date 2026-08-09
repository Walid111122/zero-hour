# Zero Hour

[![CI](https://github.com/Walid111122/zero-hour/actions/workflows/ci.yml/badge.svg)](https://github.com/Walid111122/zero-hour/actions/workflows/ci.yml)

A post-apocalyptic 4X strategy game for mobile, built solo on a near-zero budget. It pairs a
standalone lane-runner (the acquisition hook, playable on its own) with base building, hero
collection, a shared world map, alliances with voice chat, and real-time arena rooms.

**Start here → [`docs/00-README-INDEX.md`](docs/00-README-INDEX.md)**
**Track progress → [`checklists/MASTER-CHECKLIST.md`](checklists/MASTER-CHECKLIST.md)**

---

## Repository layout

| Path | What lives here |
|---|---|
| `docs/` | 29 design, engineering and operations documents. The design of record. |
| `checklists/` | Master checklist plus one detailed checklist per phase. |
| `shared/ZeroHour.Sim/` | Deterministic simulation core. netstandard2.1, no Unity, no floats. |
| `shared/ZeroHour.Sim.Tests/` | xUnit suite, including the determinism guards. |
| `client/` | Unity 6000.5.6f1 project, plus the editor-only Cline bridge. |
| `server/` | ASP.NET Core 10 API, SignalR hub, EF Core schema, and its test suite. |
| `tools/scripts/` | Build and Unity batch-mode helpers. |

## Build and test

```powershell
dotnet build ZeroHour.slnx
dotnet test  ZeroHour.slnx
```

### If every sim test fails at once on Windows

A whole-suite failure with `FileLoadException ... An Application Control policy has
blocked this file (0x800711C7)` is Windows Smart App Control refusing to load the freshly
built, unsigned `ZeroHour.Sim.dll`. It is an OS policy decision rather than a code fault —
the same commit passes in CI. Confirm it with:

```powershell
Get-WinEvent -LogName Microsoft-Windows-CodeIntegrity/Operational -MaxEvents 20
```

Run the suite in a container instead, which leaves the machine and the working tree alone:

```powershell
pwsh tools/scripts/test-sim-docker.ps1
```

Turning Smart App Control off in Windows Security under "App & browser control" also fixes
it permanently, but that switch is irreversible without reinstalling Windows, so decide
deliberately rather than as a build step.

The simulation suite is expected to stay under one second. If it creeps past that, the
feedback loop degrades and the tests stop getting run.

## The one rule that matters most

`ZeroHour.Sim` is compiled once and shared verbatim between client prediction and server
authority. For the server to be able to re-simulate a player's battle and expect an identical
result, that assembly must be perfectly deterministic. So inside it:

- no `float` or `double` — use `Fixed` (Q32.32 integer math)
- no `System.Random` — use `DetRandom` with an explicit seed
- no `DateTime.Now` — take the tick count as an input
- no `UnityEngine` reference of any kind

`DeterminismGuardTests` enforces all of this by reflection and fails the build on violation.
That is deliberate: a single stray `float` produces a desync that surfaces months later, on
one device family, in one battle out of a thousand. Catching it at commit time is far cheaper
than diagnosing it in production.

## Current status

Phase A (documentation) complete. Phase 0 largely done: the deterministic sim core, Unity
client, Cline bridge, server skeleton, Docker stack and CI are all built and verified end to
end. What remains is Android player settings and the nightly Unity CI job. See the master
checklist for the authoritative state.

## Running the stack

Verified on Docker 29.6.2: all three containers reach `healthy` and the app serves `/health`,
`/health/sim` and `/health/deep` on port 5199.

```powershell
cp .env.example .env         # fill in the CHANGE_ME values first
docker compose config        # validates without needing the daemon
docker compose up -d

# Apply the schema — see below. Needs --context because there are two DbContexts,
# and needs the connection string exported so the design-time factory can reach Postgres.
$env:ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=zerohour;Username=zerohour;Password=<from .env>"
dotnet ef database update --project server/ZeroHour.Server --context PostgresGameDbContext
```

That last step is not optional. **Nothing applies migrations on startup**, and `/health/deep`
only checks connectivity — so a freshly-started stack reports entirely healthy while the
database contains zero tables. A green health check is not evidence that the schema exists.
