# 26 — Roadmap & Phase Gates

> 11 phases. Each ends in a **gate** — a question that must be answered honestly before the next phase starts.
> Timeline assumes one developer working with Cline. Ranges, not promises.

---

## The phase table

| # | Phase | Duration | Gate question |
|---|---|---|---|
| **0** | Foundation & tooling | 1–2 wk | Can Cline build, test, and run this project end-to-end? |
| **1** | Runner MVP | 3–4 wk | **Is the 60-second loop fun with zero art?** |
| **2** | Base & idle layer | 3–4 wk | Does the player return the next day unprompted? |
| **3** | World map & marches | 4–5 wk | Does PvP feel exciting rather than punishing? |
| **4** | Alliances + ★ **voice** | 5–6 wk | Do players talk to each other, and does it change how they play? |
| **5** | ★ **Arena rooms** | 6–8 wk | Would you play a sixth match? |
| **6** | Events & live-ops | 4–5 wk | **Can a new event ship with zero client builds?** |
| **7** | Polish, art, localisation | 4–6 wk | Does it look and feel like a real product? |
| **8** | Monetization | 3–4 wk | Do purchases work and feel fair? |
| **9** | Security & hardening | 2–3 wk | Can you break your own game? |
| **10** | Soft launch → global | 4–8 wk | Do the retention gates pass? |

**Total: roughly 10–14 months** of sustained solo work. Phases 4 and 5 are the two ★ features and account for a quarter of the schedule — deliberately, since they're the reason the game exists.

---

## Phase detail

### Phase 0 — Foundation (1–2 weeks)
Repo, solution, Unity project, `shared/Sim` with fixed-point maths and deterministic RNG, CI, Docker Compose locally, and the **Cline ↔ Unity bridge** (`28`).

**Gate:** `dotnet test` passes, Unity compiles and enters play mode, CI is green, and Cline can trigger a Unity compile and read back the result.

### Phase 1 — Runner MVP (3–4 weeks)
`RunnerSim` in fixed-point, the gate/merge/shoot loop, 20 stages, boss fights, idle income, local save. Grey-box art only.

**Gate — the most important one in the project:** hand the build to five people who don't know you. Do they play more than one stage without being asked? If not, iterate here. Do not proceed. Everything downstream assumes this loop is fun.

### Phase 2 — Base & idle (3–4 weeks)
Buildings, timers, resources with the 8-hour overflow cap, research, training, heroes with gacha, server + Postgres + Redis, auth, lazy state resolution.

**Gate:** does a tester come back the next day without a push notification? That's the idle loop working.

### Phase 3 — World map & marches (4–5 weeks)
Chunked world, tiles, marches, PvP with the wounded rule and warehouse protection, shields, zombies, rallies, battle reports.

**Gate:** does losing a battle make a tester want to retaliate, or quit? If it's quit, the loss rules are too harsh — retune before proceeding.

### Phase 4 — Alliances + ★ voice (5–6 weeks)
Alliance CRUD, ranks, help, tech, gifts, chat with LibreTranslate, mail, friends. Then LiveKit, the voice widget, PTT, moderation, age gating, and the report flow.

**Gate:** in a 20-person test alliance, do people actually use voice? Does a rally with voice feel different from one without? If voice adoption is under 15%, the UX needs work before Phase 5 builds on top of it.

### Phase 5 — ★ Arena rooms (6–8 weeks)
The hardest phase. `ArenaSim`, the 20 Hz server host, prediction and reconciliation, 1v1 vertical slice, then formats, normalization, ELO, maps, AvA, replays, spectating.

**Gate:** after five sparring matches, would you play a sixth? Answer honestly. Also: does an HQ 9 player beat an HQ 24 player with better play in normalized mode?

### Phase 6 — Events & live-ops (4–5 weeks)
Event framework, server-driven UI, Arms Race, Alliance Duel, Crazy Joe, Capitol Clash, season pass, admin panel.

**Gate (hard):** create and launch a brand-new event to an already-installed client with **no client build**. If that doesn't work, live-ops is impossible and the architecture needs fixing now rather than later.

### Phase 7 — Polish & localisation (4–6 weeks)
Real art, audio, juice, 8 languages including **Arabic RTL**, accessibility, performance passes, the FTUE rebuild informed by everything learned.

**Gate:** a stranger's first 60 seconds are smooth, readable, and appealing in their own language.

### Phase 8 — Monetization (3–4 weeks)
Play Billing v7 with server-side validation, the 11 offer types, VIP with documented free paths, contextual offers, rewarded ads, alliance gifts on purchase.

**Gate:** buy something with a real card. Does it grant correctly, restore after reinstall, and produce an audit row? Does any offer feel manipulative? If so, cut it.

### Phase 9 — Security & hardening (2–3 weeks)
Full server-side validation audit, runner re-simulation, arena intent validation, rate limits, bot detection, admin panel lockdown, penetration self-test.

**Gate:** try to cheat your own game using the threat list in `20 §1`. Document what you attempted and what happened.

### Phase 10 — Launch (4–8 weeks)
Soft launch through the market ladder in `25 §3`, tuning against real data, then global.

**Gate:** the retention gates in `25 §3`. If they fail, fix and retest rather than launching anyway.

---

## Dependency graph

```
        Phase 0 ──▶ Phase 1 ──▶ Phase 2 ──▶ Phase 3
                                   │           │
                                   └──▶ Phase 4 (alliances + ★ voice)
                                              │
                                              ▼
                                        Phase 5 (★ arena)
                                              │
                        ┌─────────────────────┤
                        ▼                     ▼
                   Phase 6 (events)      Phase 7 (polish)
                        └──────────┬──────────┘
                                   ▼
                           Phase 8 → 9 → 10
```

**Hard dependencies:** ★ arena needs alliances (for sparring opponents) and voice (for the coordination that makes it worth playing). Events need most systems to exist so there's something to score. Monetization comes last because you cannot ethically or effectively sell access to a game that isn't yet good.

---

## What is explicitly deferred

| Item | Why | Revisit |
|---|---|---|
| iOS | Doubles platform work and adds a $99/yr fee | After Android proves out |
| Desert Storm-style large AvA | ★ Arena covers the need better | If AvA metrics demand it |
| Cross-state warfare | Enormous scope | Post-launch |
| Guild-vs-guild seasons beyond AvA | Needs population | Post-launch |
| Player-to-player trading | Fraud surface | Probably never |
| Cosmetic skins beyond arena | Art capacity | When there's an artist |
| Cross-alliance sparring | Matchmaking complexity | Phase 6+ |

---

## Risk register (top 5)

| Risk | Severity | Mitigation |
|---|---|---|
| Runner isn't fun (Phase 1 gate fails) | 🔴 | Iterate at Phase 1 for as long as it takes. Nothing downstream survives an unfun core |
| ★ Arena netcode defeats a solo dev | 🔴 | 1v1 vertical slice first; local sim before networking; formats scale up only after stability |
| Scope creep across 11 phases | 🔴 | Gates are binary. No phase starts until the previous gate passes |
| Solo burnout over 12 months | 🟠 | Ship playable builds at every phase; the WebGL link makes progress visible and shareable |
| ★ Voice moderation liability | 🟠 | Age gate, reporting, retention limits, published policy (`22`) |

The last honest word on risk: the biggest threat to this project is not technical. It is 14 months of solo effort without external validation. That's precisely why every phase gate is "give it to a real person and watch them play" rather than "is the code done."

---

## Next
- `checklists/MASTER-CHECKLIST.md`
- `checklists/phase-0-foundation.md`
- `28-cline-workflow.md`
