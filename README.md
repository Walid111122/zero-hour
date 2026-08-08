# Zero Hour

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
| `client/` | Unity project *(Phase 0, not yet created)*. |
| `server/` | ASP.NET Core services *(Phase 0, not yet created)*. |
| `tools/scripts/` | Build and Unity batch-mode helpers *(Phase 0)*. |

## Build and test

```powershell
dotnet build ZeroHour.slnx
dotnet test  ZeroHour.slnx
```

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

Phase A (documentation) complete. Phase 0 in progress: the deterministic sim core is built and
tested; Unity client, server skeleton, Cline bridge and CI are next. See the master checklist
for the authoritative state.
