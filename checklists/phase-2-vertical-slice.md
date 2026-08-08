# Phase 2 — Vertical Slice: Base & Idle

> **Goal:** the base layer, with the server as the authority on every timer.
> **Est:** 6–8 weeks · **Docs:** `04`, `12`, `14`, `15`, `19`

**Gate to Phase 3:** a new player reaches HQ 5 without confusion · all build timers server-authoritative · offline and online resource math never diverge

---

## 2.1 Server foundation (`14`, `15`)

- [ ] Postgres schema: `players`, `player_states`, `buildings`, `jobs`, `currency_audit`
- [ ] Auth: device-ID guest accounts, JWT access + rotating refresh (`20 §3`)
- [ ] `IClock` server-synced; client time never trusted anywhere
- [ ] **Lazy state resolution:** on read, advance the state to `now` from stored timestamps
- [ ] Idempotency keys on every mutating endpoint
- [ ] `currency_audit` row written for every currency change, no exceptions
- [ ] Config service: CSV → in-memory tables, hot reload on `configVersion`
- [ ] Redis caching for hot player state

### Endpoints
- [ ] `POST /auth/guest`, `POST /auth/refresh`
- [ ] `GET  /player/state` — resolved to now
- [ ] `POST /building/upgrade` — intent only (`20 §2`)
- [ ] `POST /building/collect`
- [ ] `POST /job/speedup`
- [ ] `POST /runner/complete` — **server re-simulates the input log** (`20 §4`)

## 2.2 Base scene (Unity)

- [ ] `Base.unity`, additive, 3/4 fixed camera
- [ ] Pan + pinch-zoom, clamped bounds
- [ ] Grid placement, occupancy validation
- [ ] Tap building → radial action menu (`19 §8`)
- [ ] Build queue strip along the top
- [ ] Construction VFX and state visuals per level
- [ ] Seamless base ⇄ world zoom transition (stubbed target until Phase 4)

## 2.3 Buildings & timers (`04`)

- [ ] 8 core buildings, definitions in `data/buildings.csv`
- [ ] Upgrade costs and times from data, curve per `04 §3`
- [ ] HQ-level prerequisite gating, **enforced server-side**
- [ ] Resource generation with the 8-hour overflow cap (`04 §4`)
- [ ] Collection with capacity clamp
- [ ] Build queue: 1 free slot + 1 premium slot
- [ ] Speedups: free 5-minute, then item/diamond tiers
- [ ] Research tree scaffold
- [ ] Troop training scaffold (full system in Phase 3)

## 2.4 Sim additions

- [ ] `EconomySim.Accrue` — matches server exactly, used for client display
- [ ] `BuildingSim.CostFor(defId, level)` / `TimeFor(defId, level)`
- [ ] Determinism fixtures: 30 days of accrual, client vs server, identical hashes
- [ ] Test: online and offline paths produce the same balance for the same elapsed time

**This is the gate's real test.** If the client's predicted balance drifts from the server's, players see numbers change under them, and trust in the economy is gone.

## 2.5 FTUE & red dots (`19 §3`, `19 §5`)

- [ ] Tutorial driven from `data/tutorial_steps.csv`
- [ ] Runner first, base introduced after the first stage clear (`03 §4`)
- [ ] Highlight + arrow + one line of copy, one objective at a time
- [ ] Skippable from step 5
- [ ] Red-dot tree: Base, Heroes, Mail scaffold
- [ ] Every dot clearable by a free action; **no dot for a paid action**
- [ ] Push notifications for build completion (opt-in, respectful cadence)

## 2.6 Persistence & offline

- [ ] Server state is authoritative; local save is display cache only
- [ ] Offline → online reconciliation on reconnect
- [ ] Conflict resolution: server always wins, client re-renders
- [ ] Graceful offline mode: read-only, queued intents, clear UI indication

---

## Gate checklist

- [ ] Three new testers reach HQ 5 without asking a question
- [ ] Timers verified server-side: change the device clock, nothing changes in-game
- [ ] 30-day accrual fixture matches between client and server
- [ ] Kill the app mid-build; the timer is correct on return
- [ ] Every currency change has an audit row
- [ ] `GET /player/state` p95 under 200 ms locally
- [ ] Red dots all clearable; none permanently stuck

→ Next: [phase-3-core-systems.md](phase-3-core-systems.md)
