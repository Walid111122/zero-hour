# Zero Hour — Master Documentation Index

> **Codename:** Zero Hour
> **Genre:** Post-apocalyptic 4X strategy / base-builder with hyper-casual runner hook
> **Reference title studied:** *Last War: Survival* (FirstFun)
> **Engine:** Unity 6000.5.6f1 (URP, IL2CPP)
> **Server:** ASP.NET Core 10 (authoritative, modular monolith)
> **Budget:** $0 infrastructure. Only unavoidable cost is a Google Play developer account ($25 one-time), deferred.
> **Team assumption:** 1 developer (Cline) + 1 owner/designer/playtester (you)

---

## How to use these documents

1. **Start here**, then read `02-vision-scope-ladder.md` to understand what "done" means at each stage.
2. **`checklists/MASTER-CHECKLIST.md` is the single source of truth for progress.** Docs describe *what* and *why*; checklists track *done*.
3. Every doc is written to be independently readable. Cross-references use `[see NN-name]`.
4. Numbers in balance tables are **starting values, not gospel**. They are tuned from telemetry in Phase 9.
5. When a doc and the code disagree, **the code is right and the doc is a bug**. Update the doc.

---

## Document map

### Foundation
| # | Document | Purpose |
|---|---|---|
| 00 | `00-README-INDEX.md` | This file |
| 01 | `01-teardown-last-war.md` | Full competitive teardown of *Last War: Survival* — every system dissected |
| 02 | `02-vision-scope-ladder.md` | Product vision, pillars, target player, scope ladder (MVP → Live) |

### Game Design (the GDD, split for sanity)
| # | Document | Purpose |
|---|---|---|
| 03 | `03-GDD-core-loops.md` | Session loop, daily loop, weekly loop, lifetime arc |
| 04 | `04-GDD-base-buildings.md` | Every building, level tables, HQ gating, timers, queues |
| 05 | `05-GDD-troops-heroes-combat.md` | Troop triangle, tiers, hero gacha, squads, damage model |
| 06 | `06-GDD-runner-minigame.md` | The lane-runner: gates, waves, bosses, idle income math |
| 07 | `07-GDD-worldmap-marches.md` | World grid, marches, nodes, PvP, shields, teleport |
| 08 | `08-GDD-alliance-social.md` | Alliances, ranks, help, gifts, territory, chat |
| 09 | `09-GDD-events-liveops-calendar.md` | Arms Race, Alliance Duel, Crazy Joe, Desert Storm, Capitol Clash |

### New Features (your additions)
| # | Document | Purpose |
|---|---|---|
| 10 | `10-FEATURE-voice-chat.md` | ★ Alliance voice chat — channels, tech, moderation, legal |
| 11 | `11-FEATURE-arena-rooms.md` | ★ Arena rooms — intra-alliance sparring + alliance-vs-alliance war |

### Systems & Business
| # | Document | Purpose |
|---|---|---|
| 12 | `12-economy-balance-model.md` | All formulas, sinks/faucets, tuning model |
| 13 | `13-monetization-iap.md` | Packs, battle pass, VIP, gacha pity, whale design |
| 21 | `21-analytics-kpis.md` | Event taxonomy, KPI targets, dashboards |
| 25 | `25-launch-ua-plan.md` | $0 user acquisition, ASO, organic strategy |

### Engineering
| # | Document | Purpose |
|---|---|---|
| 14 | `14-tech-architecture.md` | Full system architecture, shared Sim, lazy evaluation |
| 15 | `15-data-schema.md` | Postgres DDL, Redis key map, save format |
| 16 | `16-unity-project-standards.md` | Folders, asmdefs, naming, perf budgets, code style |
| 17 | `17-netcode-protocol.md` | Opcodes, framing, reconnection, arena tick model |
| 20 | `20-security-anticheat.md` | Threat model, validation, IAP receipts, bans |
| 23 | `23-devops-ci-hosting.md` | Oracle free tier, deployment, CI/CD, backups |
| 24 | `24-qa-test-plan.md` | Test strategy, device matrix, load testing |

### Craft
| # | Document | Purpose |
|---|---|---|
| 18 | `18-art-audio-bible.md` | Art direction, CC0 asset sources, audio plan |
| 19 | `19-ux-ui-system.md` | Screen map, red-dot tree, reward juice spec |

### Operations
| # | Document | Purpose |
|---|---|---|
| 22 | `22-legal-compliance.md` | IP safety, GDPR/COPPA, voice moderation law, store policy |
| 26 | `26-ROADMAP.md` | 11-phase roadmap with durations and gates |
| 27 | `27-unity-setup-guide.md` | **Beginner Unity guide — read this before opening Unity** |
| 28 | `28-cline-unity-workflow.md` | How the AI↔Unity bridge works and how to use it |

---

## Checklists

| File | Covers |
|---|---|
| `checklists/MASTER-CHECKLIST.md` | **Everything.** Top-level progress across all phases |
| `checklists/phase-0-foundation.md` | Repo, tooling, scaffold, bridge |
| `checklists/phase-1-mvp.md` | Runner minigame + idle (first shippable game) |
| `checklists/phase-2-vertical-slice.md` | Isometric base, buildings, resources, FTUE |
| `checklists/phase-3-core-systems.md` | Troops, heroes, gacha, tech tree, combat |
| `checklists/phase-4-social-voice.md` | World map, alliances, chat, ★voice |
| `checklists/phase-5-arena.md` | ★Arena rooms (both types) |
| `checklists/phase-6-liveops-events.md` | Event framework + all events |
| `checklists/phase-7-monetization.md` | IAP, shop, pass, VIP, analytics |
| `checklists/phase-8-polish-optimization.md` | Perf, juice, localization, audio |
| `checklists/phase-9-softlaunch.md` | Beta, tuning, load test |
| `checklists/phase-10-launch-live.md` | Global launch + live operations |

---

## The three non-negotiable engineering rules

These exist because breaking them is what kills projects in this genre.

**1. The server is the only source of truth.**
The client never decides a resource total, a battle outcome, a timer completion, or a reward. It *predicts* and *displays*. If client and server disagree, the client is wrong and re-syncs. No exceptions, ever.

**2. Balance data lives outside the client build.**
Every number — build costs, troop stats, event rewards, drop rates — is data, served from the server, hot-reloadable. If you have to ship an app update to nerf a hero, you have already lost. See `12` and `14`.

**3. One simulation, two hosts.**
`shared/Sim` is a `netstandard2.1` library with **no UnityEngine references and no floating-point math**. It compiles into both the Unity client and the ASP.NET server. Combat, economy, and formulas exist exactly once in the codebase. See `14`.

---

## Legal one-liner (details in `22`)

Game **mechanics, systems, and math are not copyrightable** — reimplementing them is legitimate and standard industry practice. **Art, characters, names, icons, music, UI artwork, and store copy are protected.** Zero Hour ships 100% original or CC0/public-domain assets, an original name, and original characters. Nothing is copied, extracted, decompiled, or ripped from *Last War: Survival* or any other title.
