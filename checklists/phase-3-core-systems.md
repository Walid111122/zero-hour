# Phase 3 — Core Systems: Troops, Heroes, Combat

> **Goal:** deterministic combat that resolves identically on client and server.
> **Est:** 8–10 weeks · **Docs:** `05`, `12`, `15`, `23`

**Gate to Phase 4:** combat resolves identically on client prediction and server truth for **1,000 randomised fixtures** · gacha pity verified statistically over 100k simulated pulls

---

## 3.1 Troops (`05`)

- [ ] 3 types × 10 tiers, definitions in `data/troops.csv`
- [ ] Training: cost, time, capacity from HQ + barracks level
- [ ] Promotion between tiers with resource + time cost
- [ ] Counter triangle, advantage multiplier tunable from data
- [ ] Test: the triangle is symmetric — `Mult(A,B) * Mult(B,A) == 1`
- [ ] Troop capacity and march capacity derived from buildings/tech
- [ ] Hospital: wounded troops, heal cost and time

## 3.2 Heroes (`05`)

- [ ] Hero definitions, rarities, base stats in `data/heroes.csv`
- [ ] Level, star, skill, and gear progression tracks
- [ ] Skills with cooldowns, resolved in `Sim`
- [ ] Squad formation: 3 heroes, leader determines squad type
- [ ] Hero power contribution to total power
- [ ] Hero UI: roster, detail, upgrade, formation

## 3.3 Gacha (`05`, `13`, `22`)

- [ ] Pull logic in `Sim`, deterministic given a seed
- [ ] **Pity system** — guaranteed by pull 60
- [ ] Rates published in-app at the point of pull (legal requirement, `22 §3`)
- [ ] Duplicate → shard conversion
- [ ] Free pull cadence (daily/event) so there is always a non-paid path
- [ ] Test: 100k simulated pulls — observed rates match published rates within tolerance
- [ ] Test: pity **always** fires by 60, across 100k runs, no exceptions

The statistical test matters more than it looks. A pity bug that fires at 61 one time in ten thousand is both a legal problem and the kind of thing players notice and post about.

## 3.4 Combat resolver (`05`)

The heart of the game. Pure, fixed-point, in `shared/ZeroHour.Sim/Combat/`.

- [ ] `BattleState`, `BattleUnit`, `BattleResult` types
- [ ] Round-based resolution with deterministic ordering (no dictionary enumeration)
- [ ] Type advantage, hero skills, tech bonuses, formation applied in a fixed order
- [ ] **Wounded rule:** losses go to hospital, never exceed 30% dead (`05`)
- [ ] Warehouse-protected resources on defence (`07`)
- [ ] Battle report generation: per-round detail, losses, rewards
- [ ] Replay from seed + input log
- [ ] Client prediction path uses the identical `Sim` code

### The 1,000-fixture suite (`23 §3`)
- [ ] Fixture generator: randomised armies, heroes, tech, terrain
- [ ] Expected final-state hash recorded per fixture
- [ ] Runs on Windows x64, Android ARM64, Linux ARM64
- [ ] **All three produce byte-identical hashes**
- [ ] Wired into CI as a required job

## 3.5 Tech tree

- [ ] Economy / Battle / Growth branches, `data/tech.csv`
- [ ] Prerequisites, costs, research time
- [ ] Effects applied through the same modifier pipeline as heroes and gear
- [ ] Server-authoritative research queue

## 3.6 UI

- [ ] Troop training screen with queue
- [ ] Hero roster, detail, upgrade, gacha screens
- [ ] Formation editor
- [ ] Tech tree screen
- [ ] Battle report viewer with round-by-round breakdown
- [ ] Power breakdown panel — show players exactly where their power comes from

---

## Gate checklist

- [ ] 1,000 fixtures pass identically on all three platforms
- [ ] 100k-pull gacha test passes rates and pity
- [ ] A battle predicted client-side matches the server result exactly, 100 live runs
- [ ] Wounded rule verified: no battle ever kills more than 30%
- [ ] Battle reports readable and accurate
- [ ] Sim test suite still runs in under 1 second

→ Next: [phase-4-social-voice.md](phase-4-social-voice.md)
