# 02 — Vision, Pillars & Scope Ladder

---

## 1. The pitch

> **Zero Hour** is a post-apocalyptic survival strategy game. It opens as a 60-second squad runner anyone can play, and quietly grows into a world of bases, armies, and alliances that fight, talk, and duel in real time.

**Elevator version:** *Last War's* hook, with the two social features it forgot — **voice** and **arenas**.

---

## 2. Product vision

Three sentences that every design decision must serve:

1. **A new player has fun in under 30 seconds** — no tutorial wall, no reading, one input.
2. **A returning player always has something waiting** — offline income, a finished timer, an alliance request, an event.
3. **A committed player's most valuable possession is their alliance** — not their base, not their heroes. **Their people.**

Point 3 is the differentiator. Bases can be rebuilt and heroes can be re-rolled, but a player who has spent 200 hours *talking* to the same 40 people in voice chat, sparring with them in the arena, and fighting rival alliances alongside them does not churn. That is the whole strategy.

---

## 3. Design pillars

Ranked. When two pillars conflict, the higher one wins.

### Pillar 1 — Instant Fun, Deferred Depth
Complexity is revealed on a schedule, never dumped. The runner is playable in 3 seconds. Base building appears at minute 3. Alliances at minute 15. The world map at HQ 5. Arenas at HQ 8.
*Test:* can someone who does not read English play stage 1 successfully?

### Pillar 2 — Your Alliance Is the Game
Every major system has an alliance dimension. Progression is meaningfully faster with an alliance. Voice, arenas, help, gifts, tech, war.
*Test:* does a solo player feel the absence of an alliance within their first hour?

### Pillar 3 — Never Lose What You Built
Wounded, not dead. Protected warehouse. Shields. Zero-loss sparring. Defeat costs progress, never possessions.
*Test:* can a player be attacked 10 times overnight and still want to open the app?

### Pillar 4 — Time Is the Currency, Fairly Priced
We sell time. We do not sell power that is otherwise unreachable. Every advantage is earnable for free, just slower.
*Test:* can a determined F2P player reach the top 100 in a season?

### Pillar 5 — The Server Is the Truth
No client authority, ever. Balance lives in data. Events launch without app updates.
*Test:* can we nerf a hero and launch an event today, with no store release?

---

## 4. Target player

| Segment | % of base ⚠️ | What they come for | What keeps them |
|---|---|---|---|
| **Casual runner** | ~50% | The ad, the runner | Idle income, stage progression |
| **Collector / builder** | ~30% | Base building, heroes | Timers, completion, gacha |
| **Competitor** | ~15% | PvP, rank | Arenas, leaderboards, war |
| **Socialite / leader** | ~5% | Community, status | Voice chat, alliance politics, President title |

**The socialite segment is small but critical.** They are your alliance leaders. They recruit, organise, and retain everyone else. Voice chat is built primarily *for them* — and they will drag the other 95% into staying.

**Primary markets for v1:** MENA (Arabic), LATAM (Spanish/Portuguese), Turkey, SE Asia, Russia. Reasoning: high engagement with alliance-driven strategy games, lower UA competition than US/EU, and strong voice-chat cultural fit. **Arabic RTL support is a launch requirement, not a nice-to-have.**

---

## 5. Scope ladder

The single most important section in this document. Each rung is a **complete, shippable product**. You may stop at any rung and still have something real.

### Rung 1 — "The Runner" (Phase 0–1) · ~6–9 weeks
**What it is:** a standalone hyper-casual runner with idle income.

Contains: lane runner, gates, waves, bosses, 60 stages, permanent upgrades, offline earnings, save/load, full UI, juice.
Does NOT contain: base, world map, alliances, PvP, server, monetization.

**Shippable as:** free WebGL game on itch.io + Android APK.
**Why ship it:** validates the hook with real retention data for $0. If nobody plays the runner, nobody will play the 4X.
**Success criteria:** 10+ minute median session, 30%+ D1 on a small sample, 3 external testers reach stage 20 unprompted.

---

### Rung 2 — "The Survivor" (Phase 2–3) · ~+14–18 weeks
**What it is:** a single-player base builder with the runner attached.

Adds: isometric base, 12 buildings, resources, timers, HQ gating, troops, heroes, gacha, tech tree, server-authoritative state, accounts.
Does NOT contain: world map, alliances, PvP, arenas, voice.

**Shippable as:** Android soft launch, single-player.
**Success criteria:** median player reaches HQ 10 · D7 ≥ 12% · no economy exploit found in 2 weeks of testing.

---

### Rung 3 — "The Alliance" (Phase 4) · ~+8–10 weeks
**What it is:** the full multiplayer 4X, and where Zero Hour becomes itself.

Adds: world map, marches, PvP, alliances, ranks, help, gifts, tech, territory, chat + translate, mail, **★ voice chat**.

**Shippable as:** open beta, one server.
**Success criteria:** 70%+ of active players in an alliance · voice chat used by 40%+ of alliance members weekly · D7 ≥ 15%.

---

### Rung 4 — "The Arena" (Phase 5) · ~+6–8 weeks
**What it is:** real-time competitive combat, your second differentiator.

Adds: **★ intra-alliance sparring** (1v1, 3v3, FFA, KotH, ELO ladder, zero-loss, power normalization) and **★ alliance-vs-alliance war rooms** (5v5/10v10/20v20, Deathmatch/Objective/Boss Race, brackets, war points).

**Success criteria:** 50%+ weekly participation in sparring · every active alliance runs at least one AvA match per week · replay system reproduces matches exactly.

---

### Rung 5 — "The Live Game" (Phase 6–10) · ~+20–25 weeks
Adds: full event calendar, admin panel, monetization, polish, localization, launch.

**This is where the game becomes a business.**

---

## 6. Explicit non-goals for v1

Writing these down is what protects the schedule. Each is a real feature that we are deliberately not building.

| Not doing | Why | Revisit |
|---|---|---|
| iOS | $99/yr against a $0 budget | After first revenue |
| Cross-server warfare | Needs many populated servers first | Phase 11+ |
| Drone progression track | An entire parallel system | Phase 6+ |
| Landscape orientation | Doubles UI work | Never (portrait is correct for the genre) |
| Real-time base-vs-base combat | Netcode cost; resolved combat is genre-correct | Never |
| Player-created content | Moderation burden | Never |
| Console / PC-native | Wrong market | Never |
| NFT / crypto anything | No | Never |
| More than 3 troop types | Balance surface explodes | Phase 8+ maybe |
| Guild-vs-guild territory war on the world map | Arena rooms cover the need better and cheaper | Phase 11+ |

**Rule:** adding anything to this project requires removing something of equal size, or explicitly moving the phase gate. No silent scope growth.

---

## 7. Definition of Done — per rung

A rung is done when **all** of these are true. Not "mostly", not "except one thing".

| # | Criterion |
|---|---|
| 1 | Zero compile errors, zero new warnings |
| 2 | All checklist items in the phase file are `[x]` |
| 3 | Runs on a real Android device, not just the editor |
| 4 | 30 minutes of play with no crash, softlock, or visual break |
| 5 | Server-authoritative for everything that affects progression |
| 6 | All balance values in data files, none hardcoded |
| 7 | `shared/Sim` unit tests green |
| 8 | Someone who is not you played it and understood it without help |
| 9 | The relevant `docs/` file matches the code |
| 10 | The phase gate criteria in `MASTER-CHECKLIST.md` pass |

---

## 8. Naming

**Codename:** `Zero Hour`
**Namespace root:** `ZeroHour`
**Unity product name:** set in `ProjectSettings` and mirrored in `Assets/_Project/Settings/GameIdentity.asset`

Renaming later touches exactly three places: the identity asset, the `ProjectSettings` product name, and a namespace find/replace. A script at `tools/scripts/rename-project.ps1` will do all three. **Do not scatter the name through the codebase.**

Before launch, run a trademark search on the final name in your target markets. See `22-legal-compliance.md`.

---

## 9. What success looks like

| Horizon | Target | Meaning |
|---|---|---|
| **Rung 1** | 100 people play the runner | The hook works |
| **Rung 2** | D7 ≥ 12% | The progression loop works |
| **Rung 3** | 70% in alliances, voice used weekly | The social layer works |
| **Rung 4** | 50% weekly arena participation | The differentiator works |
| **Rung 5** | ARPDAU ≥ $0.15, D30 ≥ 6% | It is a business |
| **Year 2** | Free tier exceeded, servers paid from revenue | It is sustainable |

**The honest bar:** if you reach Rung 3 with a healthy D7, you have built something the vast majority of solo attempts in this genre never reach. Everything past that is upside.

---

## Next
- `03-GDD-core-loops.md` — the loops that make all of this run
- `26-ROADMAP.md` — phase-by-phase schedule
