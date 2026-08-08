# Zero Hour — MASTER CHECKLIST

> **This file is the single source of truth for project progress.**
> Each phase links to a detailed checklist. Update the box the moment a thing is genuinely done.
>
> **Definition of Done (applies to every item):**
> 1. Code compiles with zero errors and zero new warnings
> 2. Feature works in Unity Play Mode
> 3. Server-authoritative where applicable (client cannot cheat it)
> 4. Balance values live in data, not hardcoded
> 5. Unit tests pass for any `shared/Sim` logic
> 6. The relevant doc in `docs/` is accurate

**Legend:** `[ ]` not started · `[~]` in progress · `[x]` done · `[!]` blocked · `[-]` cut from scope

---

## Progress Summary

| Phase | Name | Est. | Status | Detail |
|---|---|---|---|---|
| **A** | Documentation & Design | 1 session | `[x]` | this file + `docs/` (29 docs) |
| **0** | Foundation & Tooling | 2–3 wk | `[ ]` | [phase-0](phase-0-foundation.md) |
| **1** | MVP — Runner + Idle | 4–6 wk | `[ ]` | [phase-1](phase-1-mvp.md) |
| **2** | Vertical Slice — Base | 6–8 wk | `[ ]` | [phase-2](phase-2-vertical-slice.md) |
| **3** | Core Systems | 8–10 wk | `[ ]` | [phase-3](phase-3-core-systems.md) |
| **4** | World + Social + ★Voice | 8–10 wk | `[ ]` | [phase-4](phase-4-social-voice.md) |
| **5** | ★Arena Rooms | 6–8 wk | `[ ]` | [phase-5](phase-5-arena.md) |
| **6** | Live-Ops & Events | 6–8 wk | `[ ]` | [phase-6](phase-6-liveops-events.md) |
| **7** | Monetization | 4–5 wk | `[ ]` | [phase-7](phase-7-monetization.md) |
| **8** | Polish & Optimization | 5–6 wk | `[ ]` | [phase-8](phase-8-polish-optimization.md) |
| **9** | Soft Launch | 4–6 wk | `[ ]` | [phase-9](phase-9-softlaunch.md) |
| **10** | Launch & Live Ops | ongoing | `[ ]` | [phase-10](phase-10-launch-live.md) |

**Overall: 0 / 11 phases complete**

---

## PHASE GATES — do not start phase N+1 until these pass

Scope creep is the number one killer of projects in this genre. These gates are the defense.

- **Gate 0 → 1:** Unity opens the project with zero errors · Cline Bridge reports console logs to file · `dotnet build` succeeds on the full solution · git repo initialised with LFS
- **Gate 1 → 2:** Runner is fun for 10+ minutes without changes · WebGL build playable in a browser · 3 external playtesters completed 10 stages · D1 loop (idle claim) works across an app restart
- **Gate 2 → 3:** A new player reaches HQ 5 without confusion · all build timers server-authoritative · offline/online resource math never diverges
- **Gate 3 → 4:** Combat resolves identically on client prediction and server truth for 1,000 randomised fixtures · gacha pity verified statistically over 100k simulated pulls
- **Gate 4 → 5:** 100 simulated players on one world map with no desync · voice chat holds 10 concurrent talkers for 30 min without a leak · alliance help/gift loops complete
- **Gate 5 → 6:** 10v10 arena runs 5 minutes at 20 Hz with <150 ms perceived latency on 4G · replays reproduce exactly from seed + input log
- **Gate 6 → 7:** A brand-new event can be launched from the admin panel with no client build
- **Gate 7 → 8:** IAP receipt validation rejects a forged receipt · analytics funnel shows install → tutorial → first purchase
- **Gate 8 → 9:** 60 fps sustained on a 4-year-old midrange Android · < 220 MB memory · < 150 MB download size
- **Gate 9 → 10:** D1 ≥ 35%, D7 ≥ 15% · crash-free sessions ≥ 99.5% · zero P0 bugs open

---

## PHASE A — Documentation & Design `[x]`

### Foundation docs
- [x] `docs/00-README-INDEX.md` — master index
- [x] `checklists/MASTER-CHECKLIST.md` — this file
- [x] `docs/01-teardown-last-war.md` — full competitive teardown
- [x] `docs/02-vision-scope-ladder.md` — vision, pillars, scope ladder

### Game design docs
- [x] `docs/03-GDD-core-loops.md`
- [x] `docs/04-GDD-base-buildings.md`
- [x] `docs/05-GDD-troops-heroes-combat.md`
- [x] `docs/06-GDD-runner-minigame.md`
- [x] `docs/07-GDD-worldmap-marches.md`
- [x] `docs/08-GDD-alliance-social.md`
- [x] `docs/09-GDD-events-liveops-calendar.md`

### ★ New feature specs
- [x] `docs/10-FEATURE-voice-chat.md`
- [x] `docs/11-FEATURE-arena-rooms.md`

### Business & systems
- [x] `docs/12-economy-balance-model.md`
- [x] `docs/13-monetization-iap.md`
- [x] `docs/21-analytics-kpis.md`
- [x] `docs/25-launch-ua-plan.md`

### Engineering docs
- [x] `docs/14-tech-architecture.md`
- [x] `docs/15-data-schema.md`
- [x] `docs/16-netcode-realtime.md`
- [x] `docs/17-unity-project-structure.md`
- [x] `docs/20-security-anticheat.md`
- [x] `docs/23-qa-testing.md`
- [x] `docs/24-devops-deployment.md`

### Craft docs
- [x] `docs/18-art-audio-pipeline.md`
- [x] `docs/19-ux-ui-system.md`

### Operations docs
- [x] `docs/22-legal-compliance.md`
- [x] `docs/26-ROADMAP.md`
- [x] `docs/27-unity-editor-guide.md` — **beginner guide, you read this**
- [x] `docs/28-cline-workflow.md`

### Phase checklists
- [x] `checklists/phase-0-foundation.md`
- [x] `checklists/phase-1-mvp.md`
- [x] `checklists/phase-2-vertical-slice.md`
- [x] `checklists/phase-3-core-systems.md`
- [x] `checklists/phase-4-social-voice.md`
- [x] `checklists/phase-5-arena.md`
- [x] `checklists/phase-6-liveops-events.md`
- [x] `checklists/phase-7-monetization.md`
- [x] `checklists/phase-8-polish-optimization.md`
- [x] `checklists/phase-9-softlaunch.md`
- [x] `checklists/phase-10-launch-live.md`

> **Note on doc numbering:** the engineering docs landed at `16-netcode-realtime`,
> `17-unity-project-structure`, `23-qa-testing`, and `24-devops-deployment` rather than
> the working titles originally sketched here. `docs/00-README-INDEX.md` is authoritative.

---

## PHASE 0 — Foundation & Tooling `[ ]`
*Goal: a repo that builds, a Unity project that opens clean, and an AI↔Unity feedback loop.*

- [ ] Git repo initialised, `.gitignore` (Unity + .NET), `.gitattributes` (LFS for art/audio)
- [ ] `ZeroHour.sln` with all projects
- [ ] `shared/Sim` — netstandard2.1, no UnityEngine, no float
- [ ] `shared/Sim.Tests` — xUnit
- [ ] `client/` — Unity project, URP, portrait, Android + WebGL targets
- [ ] Unity assembly definitions (asmdef) per module
- [ ] `server/ZeroHour.Server` — ASP.NET Core 10, health endpoint, WebSocket echo
- [ ] **Cline Bridge** — console log → file
- [ ] **Cline Bridge** — command file → editor action → result file
- [ ] Unity CLI batch-mode scripts (`tools/scripts/*.ps1`)
- [ ] Local dev DB (SQLite for dev, Postgres-ready)
- [ ] GitHub Actions CI (GameCI + dotnet build + tests)
- [ ] You open Unity successfully and see the project

→ **Detail: [phase-0-foundation.md](phase-0-foundation.md)**

---

## PHASE 1 — MVP: Runner + Idle `[ ]`
*Goal: a complete, shippable, genuinely fun game. This is the acquisition hook and it stands alone.*

- [ ] Lane-runner core: squad movement, drag/steer input
- [ ] Gate system: `+N`, `×N`, `−N`, `÷N`, weapon upgrade, type swap
- [ ] Unit spawning, formation, auto-fire
- [ ] Zombie waves, mid-boss, HP-bar wall boss
- [ ] Stage progression + difficulty curve
- [ ] Idle/offline income tied to highest stage
- [ ] Save/load (local + server-ready)
- [ ] Full UI: HUD, stage select, results, offline-earnings popup
- [ ] Upgrade screen (squad size, damage, fire rate)
- [ ] Juice pass: hit feedback, screen shake, numbers, SFX
- [ ] WebGL build published to a free host
- [ ] Playtested by 3+ real people

→ **Detail: [phase-1-mvp.md](phase-1-mvp.md)**

---

## PHASE 2 — Vertical Slice: Base `[ ]`
- [ ] Isometric base scene, camera, pinch-zoom
- [ ] Seamless base ⇄ world-map zoom transition
- [ ] 8 core buildings placeable and upgradeable
- [ ] Resource generation + collection + capacity
- [ ] Build queue + timers + speedups
- [ ] HQ-level gating enforced server-side
- [ ] Server-authoritative timers with lazy evaluation
- [ ] FTUE / tutorial flow
- [ ] Red-dot notification tree

→ **Detail: [phase-2-vertical-slice.md](phase-2-vertical-slice.md)**

---

## PHASE 3 — Core Systems `[ ]`
- [ ] 3 troop types × 10 tiers, training + promotion
- [ ] Counter triangle with tunable advantage
- [ ] Hero roster, rarities, level / star / skill / gear
- [ ] Gacha with pity + published rates
- [ ] Squad formation (3 heroes, leader sets type)
- [ ] Tech tree (Economy / Battle / Growth)
- [ ] Hospital + wounded troops
- [ ] Deterministic combat resolver in `shared/Sim`
- [ ] Battle replay from seed + input log
- [ ] Client prediction matches server for 1,000 fixtures

→ **Detail: [phase-3-core-systems.md](phase-3-core-systems.md)**

---

## PHASE 4 — World + Social + ★Voice `[ ]`
- [ ] World grid map, chunked streaming, spatial index
- [ ] Marches, queues, travel time, recall
- [ ] Resource nodes, zombie units, bosses
- [ ] PvP attack/scout, shields, teleports
- [ ] Alliance create/join, ranks R1–R5
- [ ] Alliance help, gifts, donations, shop
- [ ] Alliance territory, flags, turrets
- [ ] Chat: world / alliance / private + LibreTranslate
- [ ] Mail system
- [ ] ★ Voice: `IVoiceService` abstraction
- [ ] ★ Voice: alliance main channel, PTT, 8-speaker cap
- [ ] ★ Voice: rally / officer / ad-hoc channels
- [ ] ★ Voice: permissions, mute, block, report
- [ ] ★ Voice: age gate + consent + retention policy
- [ ] ★ Voice: self-hosted LiveKit deployed

→ **Detail: [phase-4-social-voice.md](phase-4-social-voice.md)**

---

## PHASE 5 — ★Arena Rooms `[ ]`
- [ ] 20 Hz authoritative tick service
- [ ] Intent-based client input, delta snapshots, interpolation
- [ ] Room lifecycle: create, lobby, ready-check, battle, results
- [ ] **Intra-alliance:** 1v1, 3v3, FFA-8, King of the Hill
- [ ] **Intra-alliance:** zero-loss sparring guarantee
- [ ] **Intra-alliance:** optional power normalization
- [ ] **Intra-alliance:** weekly ELO ladder + Champion title
- [ ] **AvA:** 5v5 / 10v10 / 20v20
- [ ] **AvA:** challenge → accept → schedule → roster lock
- [ ] **AvA:** Deathmatch / Objective Control / Boss Race modes
- [ ] **AvA:** bracket tournament (8/16 alliances)
- [ ] **AvA:** war points, leaderboard, winner buff
- [ ] Voice auto-join per room
- [ ] Replay recording + playback
- [ ] Anti-abuse: cooldowns, forfeit penalties, reward caps
- [ ] Disconnect → AI takeover + 60 s reconnect

→ **Detail: [phase-5-arena.md](phase-5-arena.md)**

---

## PHASE 6 — Live-Ops & Events `[ ]`
- [ ] Data-driven event framework + server-driven UI
- [ ] Admin panel (event scheduling, player lookup, grants)
- [ ] Arms Race (6 rotating phases)
- [ ] Alliance Duel (6-day weekly)
- [ ] Crazy Joe (wave defense)
- [ ] Desert Storm (AvA battlefield)
- [ ] Capitol Clash (President + Ministers + buffs)
- [ ] Daily login, 8-day new-server ladder, bounty tasks
- [ ] Lucky wheel, truck rescue, marshal boss
- [ ] Season / battle pass framework
- [ ] Launch a brand-new event with no client build ✅ gate

→ **Detail: [phase-6-liveops-events.md](phase-6-liveops-events.md)**

---

## PHASE 7 — Monetization `[ ]`
- [ ] Unity IAP integrated, products defined
- [ ] Server-side receipt validation (Google/Apple)
- [ ] Starter pack, growth fund, monthly cards
- [ ] Battle pass / season pass
- [ ] VIP ladder 1–20
- [ ] Diamond store + resource bundles
- [ ] State-triggered contextual offers
- [ ] Alliance gift multiplier on purchase
- [ ] GameAnalytics + PostHog wired
- [ ] KPI dashboard live (D1/D7/D30, ARPDAU, conversion)

→ **Detail: [phase-7-monetization.md](phase-7-monetization.md)**

---

## PHASE 8 — Polish & Optimization `[ ]`
- [ ] 60 fps on midrange Android (perf budget met)
- [ ] Memory < 220 MB, download < 150 MB
- [ ] Addressables + on-demand download
- [ ] Object pooling everywhere hot
- [ ] GPU instancing / batching for world map units
- [ ] Full juice pass on every reward moment
- [ ] Audio: music, SFX, mixer, ducking
- [ ] Localization: EN, AR, ES, PT, RU, ZH, DE, FR, TR, ID
- [ ] RTL layout support for Arabic
- [ ] Accessibility: colorblind-safe, scalable text, no colour-only signals
- [ ] Crash-free sessions ≥ 99.5%

→ **Detail: [phase-8-polish-optimization.md](phase-8-polish-optimization.md)**

---

## PHASE 9 — Soft Launch `[ ]`
- [ ] Google Play developer account ($25) — *the only real cost*
- [ ] Store listing, ASO, screenshots, trailer
- [ ] Closed alpha (20 testers)
- [ ] Closed beta (200 testers)
- [ ] Load test: 1,000 concurrent simulated players
- [ ] Backup + restore drill verified
- [ ] Balance retune from real telemetry
- [ ] Bug triage to zero P0/P1
- [ ] D1 ≥ 35%, D7 ≥ 15% ✅ gate

→ **Detail: [phase-9-softlaunch.md](phase-9-softlaunch.md)**

---

## PHASE 10 — Launch & Live `[ ]`
- [ ] Global release
- [ ] Content cadence established (weekly events, monthly season)
- [ ] Community: Discord, socials, support inbox
- [ ] On-call / incident runbook
- [ ] Scale-out plan executed when free tier is exceeded
- [ ] Post-launch roadmap (new heroes, cross-server, new modes)

→ **Detail: [phase-10-launch-live.md](phase-10-launch-live.md)**

---

## Running risk register

| # | Risk | Severity | Mitigation | Status |
|---|---|---|---|---|
| R1 | Scope creep kills the project | 🔴 Critical | Hard phase gates above; nothing from N+1 before N ships | Active |
| R2 | $0 UA in the most expensive genre on mobile | 🔴 Critical | Runner is the organic hook; short-form video + ASO (`25`) | Active |
| R3 | Free tier ceiling (~1–2k DAU) | 🟡 Medium | Scale-out path documented; revenue arrives before ceiling | Active |
| R4 | Art quality at $0 | 🟡 Medium | One consistent CC0 style + strong lighting beats mismatched assets | Active |
| R5 | Voice chat moderation liability | 🟠 High | Age gate, reporting, retention policy (`10`, `22`) | Active |
| R6 | Determinism drift client vs server | 🟠 High | Fixed-point math, no float in `Sim`, 1,000-fixture test suite | Active |
| R7 | Solo-dev burnout over 14–18 months | 🟠 High | Ship at Phase 1; every phase independently valuable | Active |
| R8 | IP infringement claim | 🟡 Medium | Original name/art/characters only; `22` compliance review each phase | Active |
