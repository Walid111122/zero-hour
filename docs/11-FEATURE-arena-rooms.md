# 11 — ★ FEATURE: Arena Rooms

> **Your feature #2.** Phase 5.
> Two things the reference game cannot do: **fight your own alliance mates safely**, and **fight another alliance right now**.

---

## 1. Why this feature exists

### Gap A — you cannot fight your own alliance
In this genre, alliance mates are permanent allies you *never* interact with combatively. There is no sparring, no internal ranking, no way to test a formation, no way to settle an argument about who is actually better. A 60-person social group with a combat game between them and **no way to compete inside it**. That is a large, obvious missed engagement opportunity.

### Gap B — alliance-vs-alliance combat is slow and asynchronous
AvA in the reference game means scheduled, march-based, hours-long events. There is no fast, direct, "let's fight right now" outlet. Rivalries build up with nowhere to go.

### What arena rooms add

| | |
|---|---|
| **Risk-free competition** | Sparring costs nothing and loses nothing. Play as much as you want |
| **Skill over wallet** | Power-normalized mode means a good F2P player can beat a whale. **This is the single most requested and least-delivered thing in the genre** |
| **Always-available content** | Something to do between events, at any hour, at any progression stage |
| **Internal social structure** | Ladders, champions, titles, bragging rights inside the alliance |
| **Instant AvA** | Alliance rivalry with a fast, decisive outlet |
| **Makes voice valuable** | A live match is something worth *talking over*. The two features multiply each other |

---

## 2. Two modes

| | **Sparring** (intra-alliance) | **AvA War Rooms** (alliance vs alliance) |
|---|---|---|
| Opponents | Your own alliance mates | Another alliance |
| Unlock | HQ 8 | HQ 12 |
| Stakes | **Zero** — no troop loss, no resource loss | War Points, alliance rank, pride |
| Rewards | Valor, EXP, ELO, daily bonus | War Points, Valor, alliance gifts, season rank |
| Formats | 1v1, 3v3, FFA (up to 8), King of the Hill | 5v5, 10v10, 20v20 |
| Duration | 2–4 min | 5–10 min |
| Availability | Always | Scheduled (Wed/Fri) + on-demand challenge |
| Power | Normalized **or** raw (room setting) | Raw (it's real war) |

---

## 3. Sparring (intra-alliance)

### 3.1 Room creation
```
Alliance Center → Arena → Create Room
  Format        : 1v1 / 3v3 / FFA / King of the Hill
  Power mode    : Normalized (default) / Raw
  Map           : Ruins / Highway / Rooftops / Bunker
  Entry         : Open to alliance / Invite only
  Best of       : 1 / 3 / 5
  ★ Voice       : auto-create room voice channel (default on)
```
Rooms appear in an alliance room list with a live participant count. R3+ can create; anyone can join an open room.

### 3.2 Power normalization — the important part

```
Normalized mode:
  All participants' effective stats scale to a fixed reference power P_ref
  scale = P_ref / playerPower
  troopCount_effective = troopCount · scale     (clamped 0.5× – 2.0×)
  heroStats            = reference values for that hero at the room's tier
  Composition, counters, hero identity, and skill are PRESERVED
  Raw stat advantage is REMOVED
```

**What this achieves:** a level-8 F2P player and a level-25 whale enter the same fight and the outcome is decided by **composition and decision-making**. Nothing else in the genre offers this.

**Why it's safe for monetization:** normalized sparring gives **no power rewards** — only Valor, EXP, ELO, and titles. Whales still dominate the world map, the leaderboards, and everything that produces material advantage. Sparring is a skill sandbox, deliberately walled off from the power economy. Whales get status through world dominance; F2P players get status through skill. Both are served, neither is undermined.

### 3.3 Formats

**1v1** — 2 min, best-of-1/3/5. The purest test. Feeds the ELO ladder.

**3v3** — 3 min. Three players per side, each controlling one squad. Requires coordination, which is what makes ★ voice immediately valuable.

**FFA (up to 8)** — 4 min. Last squad standing. Chaotic, fun, low-pressure, great for casual participation.

**King of the Hill** — 4 min. Hold a central point to accumulate points. First to 100 wins. Rewards positioning over raw damage, and produces the most dramatic comebacks.

### 3.4 The ELO ladder
```
Expected  = 1 / (1 + 10^((opponentElo - playerElo) / 400))
NewElo    = playerElo + K · (actual - Expected)      K = 32 (24 above 1800)
Start     = 1200
Floor     = 800  (cannot tank below this)
```
- Weekly reset to `1200 + (elo - 1200) · 0.5` — soft reset, preserves some standing
- Alliance leaderboard, visible to all members
- Sunday winner gets the **"Alliance Champion"** title for the week: a profile frame, a chat badge, and a small non-combat buff (+3% resource production). Recorded permanently in the alliance Hall of Fame.
- Rewards scale with tier: Bronze / Silver / Gold / Platinum / Champion

### 3.5 Zero-loss guarantee
**Nothing is lost in sparring. Ever.**
- No troop deaths, no wounds, no healing cost
- No resource loss
- No shield break
- No cooldown other than a 30 s inter-match breather

Daily rewards are capped (first 3 matches give bonus Valor) so the reward economy stays sane, but **play itself is unlimited**. This is deliberate: a free, unlimited, skill-based activity is exactly what a F2P player needs to feel they belong here.

---

## 4. AvA War Rooms

### 4.1 Challenge flow
```
R5/R4 → Arena → Challenge Alliance
   → pick target alliance (filtered to a fair power band, ±40%)
   → pick format (5v5 / 10v10 / 20v20) and a UTC time slot
   → target alliance R4+ accepts / declines / counter-proposes
   → both sides register rosters (locked 10 min before start)
   → ★ war voice channels open for both sides at T−15 min
   → match runs
   → War Points awarded, alliance gifts drop for the winner
```
Scheduled matches Wed/Fri; on-demand challenges any time. Weekly bracket tournament (8 or 16 alliances) on Fridays.

### 4.2 Match types

**Deathmatch** — eliminate the enemy roster. 5 min. Simple, readable, decisive.

**Objective Control** — 3 capture points, hold to accumulate points, first to 500. 8 min. The most tactical format and the one that rewards voice coordination most heavily.

**Boss Race** — both alliances fight the same NPC boss in parallel arenas; most damage in 6 min wins. **No direct PvP**, which makes it excellent for alliances with a wide power spread — everyone contributes something.

### 4.3 Rosters & substitution
- Roster size = format size; up to 5 substitutes
- Each participant brings **one squad** (3 heroes + troops)
- Raw power, no normalization — this is real war
- Late joiners may fill an empty slot up to T+60 s
- Disconnect → an AI takes over the squad defensively (see §5.5)

### 4.4 War Points & season
```
WarPoints = base(format) · outcomeMult · (1 + performanceBonus)
   win = 1.0, narrow loss = 0.3, forfeit = 0
```
Feeds the alliance shop, the seasonal AvA leaderboard, and the permanent alliance war record on the profile. **Nothing material is looted from the losing alliance** — losing costs standing, never assets. Pillar 3 applies to alliances as well as players.

---

## 5. Real-time combat design

This is a genuinely different combat system from world-map resolved combat, and it needs to be designed as one.

### 5.1 What the player actually does
Arena combat is **squad-level tactical, not unit-level RTS**. Micro-managing 20 individual units on a phone is not viable.

Per player, per match, the inputs are:
| Input | Effect |
|---|---|
| **Move order** (tap a location) | Squad moves as a formation |
| **Attack order** (tap an enemy) | Squad focuses that target |
| **Formation** (3 presets) | Aggressive / Balanced / Defensive — changes spacing and engagement range |
| **Hero skill** (up to 3 buttons) | Manual activation, on cooldown. **The main skill expression** |
| **Retreat** | Withdraw to regroup (available once per match) |

That's it. Five interaction types, all one-tap, all thumb-reachable. A player is competent in one match.

### 5.2 Netcode model

**Server-authoritative with client prediction.** Full detail in `16-netcode-realtime.md`.

```
Server: fixed 20 Hz tick, runs shared/Sim ArenaSim
Client: sends INPUT INTENTS only (never positions, never damage)
Server: simulates, broadcasts delta snapshots at 10 Hz
Client: interpolates between snapshots, predicts own squad locally,
        reconciles on mismatch (smooth correction, never a snap)
```

**Bandwidth:**
```
Snapshot ≈ 20 entities × 12 bytes ≈ 240 B + header
10 Hz → ~3 KB/s per client
20v20 (40 clients) → ~120 KB/s total ≈ 1 Mbps for the whole match
```
Trivially affordable on the free VM. Ten concurrent 20v20 matches is ~10 Mbps.

### 5.3 Determinism reuse
`ArenaSim` lives in `shared/Sim` alongside `RunnerSim` and `BattleResolver`, and shares the **same stat model, the same counter triangle, and the same damage formula** as world-map combat (`05 §5`). Only the time model differs: resolved combat runs 30 discrete rounds instantly; arena combat runs 20 ticks per second.

**Why this matters:** one balance source of truth. When you retune the counter triangle, both systems change together. If arena had its own combat maths, you would be balancing two games.

### 5.4 Replays
```
Replay = { matchId, initialState, inputLog[], seed, tickRate }
```
Because `ArenaSim` is deterministic, the input log alone reproduces the match exactly. Storage is a few KB per match.
- Watchable in-app, shareable to alliance chat
- **Doubles as the anti-cheat audit trail** — any suspicious match can be re-simulated server-side
- Feeds the admin panel match viewer

### 5.5 Disconnect handling
| Situation | Handling |
|---|---|
| Brief drop (< 30 s) | Reconnect and resume; squad held in defensive AI meanwhile |
| Long drop | AI plays defensively for the remainder |
| Sparring disconnect | Match voided if within the first 30 s (no ELO change); otherwise counted |
| AvA disconnect | Substitute may take the slot at the next respawn window |
| Server crash mid-match | Match voided, no ELO/War Point change, entry refunded |

**Explicit rule: a disconnect never costs troops or resources.** In sparring nothing is at stake anyway; in AvA only standing is at stake. This removes the entire class of "I lost my army to a bad connection" complaints, which is the most legitimate grievance players have in real-time mobile combat.

---

## 6. Maps

4 maps at launch, ~60×60 units, symmetric.

| Map | Character |
|---|---|
| **Ruins** | Dense cover, close-quarters, favours Tank |
| **Highway** | Long open lanes, favours Missile |
| **Rooftops** | Multi-level with gaps, favours Air |
| **Bunker** | Chokepoints and corridors, favours coordination over composition |

Each map favours a different troop type, so map choice is a real strategic layer and no single composition dominates. Maps are data-driven (`arena_maps.csv` + a prefab), so adding more is cheap content.

---

## 7. UX

### 7.1 Flow
```
Alliance Center → Arena
  ├─ Sparring  → room list / create / quick match / ladder
  ├─ AvA       → scheduled matches / challenges / bracket / war record
  ├─ Replays   → recent matches, alliance highlights
  └─ Ladder    → alliance ELO board, Hall of Fame
```

### 7.2 In-match HUD
- Top: score/timer/objective
- Left: your squad card (HP bar, troop count, formation toggle)
- Bottom-right: **up to 3 hero skill buttons with cooldown rings** — the primary interaction
- Bottom-left: ★ voice PTT (the widget from `10`)
- Minimap: top-right, tap to pan
- Kill/objective feed: top-centre, brief

**Design constraint: the HUD must be usable one-handed in portrait.** Every interactive element sits in the bottom third or is a tap-on-world action.

### 7.3 Spectating
Alliance members can spectate any live match in their alliance — including AvA matches they are not rostered for. Free, unlimited, and **automatically joins the match voice channel as a listener**.

This is a bigger deal than it sounds. Spectating turns a 10-person AvA match into a 60-person alliance event, and gives non-rostered players a reason to show up. It is also the cheapest engagement feature in this document.

---

## 8. Balance & fairness

| Concern | Handling |
|---|---|
| Whales dominate sparring | Normalized mode is the default |
| Sparring rewards break the economy | No power rewards; Valor/EXP/ELO only, daily bonus capped |
| ELO tanking to farm easy wins | ELO floor of 800; rewards tied to tier, not win count |
| AvA power mismatches | ±40% power band on challenges; Boss Race format for lopsided matchups |
| Alt-account farming | Same-device detection; 24 h alliance-join cooldown; ELO gains require distinct opponents |
| Sparring collusion (trading wins) | Diminishing ELO from repeated same-opponent matches (4th+ match gives 25% ELO) |
| Meta staleness | Weekly map rotation; seasonal balance passes |

---

## 9. Data files

| File | Contents |
|---|---|
| `arena_formats.csv` | formatId, teamSize, durationSec, winCondition, isAva |
| `arena_maps.csv` | mapId, size, terrain, favouredType, spawnPoints |
| `arena_normalization.csv` | referencePower per HQ band, clamp bounds |
| `arena_rewards.csv` | tier, valor, exp, dailyBonusCap |
| `arena_elo.csv` | K factor, floor, weekly reset formula, tier thresholds |
| `ava_warpoints.csv` | format, base points, outcome multipliers |

---

## 10. Implementation plan (Phase 5)

| Step | Deliverable |
|---|---|
| 1 | `ArenaSim` in `shared/Sim` — deterministic, fixed-point, unit-tested |
| 2 | Server match host: 20 Hz tick loop, room lifecycle, roster management |
| 3 | Netcode: intent messages up, delta snapshots down, prediction + reconciliation |
| 4 | Arena scene: camera, unit rendering (instanced), order input |
| 5 | Squad control: move, attack, formation, retreat |
| 6 | Hero skills with cooldowns and VFX |
| 7 | 1v1 sparring end-to-end (the vertical slice) |
| 8 | Power normalization |
| 9 | 3v3, FFA, King of the Hill |
| 10 | ELO ladder, weekly reset, Champion title, Hall of Fame |
| 11 | 4 maps |
| 12 | AvA challenge/accept/schedule flow |
| 13 | 5v5 → 10v10 → 20v20 (scale up and load test at each step) |
| 14 | Deathmatch, Objective Control, Boss Race |
| 15 | War Points, alliance war record, season leaderboard |
| 16 | Replay recording + playback |
| 17 | Spectating with voice-listener join |
| 18 | ★ Voice integration for all room types |
| 19 | Disconnect/reconnect/AI-takeover handling |
| 20 | Load test: 10 concurrent 20v20 matches on the free VM |

**Build order note:** step 7 (1v1 end-to-end) is the vertical slice. Do not build formats 9–14 until 1v1 feels good, because everything else inherits that feel.

---

## 11. Acceptance criteria
- [ ] `ArenaSim` deterministic — identical results on client, editor, and server (1,000 fixtures)
- [ ] 1v1 match completes end-to-end with correct rewards
- [ ] All 4 sparring formats playable
- [ ] All 3 AvA formats playable
- [ ] Power normalization verified: HQ 8 player beats HQ 25 player with better play
- [ ] **Zero-loss verified: troop/resource counts identical before and after sparring**
- [ ] ELO updates correctly; weekly reset works; Champion title awarded
- [ ] 20v20 stable at 20 Hz with 40 clients connected
- [ ] Latency: order issued → visible response < 150 ms on 4G
- [ ] Bandwidth < 5 KB/s per client
- [ ] Replays reproduce matches exactly
- [ ] Spectating works, including voice-listener join
- [ ] Disconnect → AI takeover → reconnect all work; nothing is ever lost
- [ ] 10 concurrent 20v20 matches run on the free VM within CPU budget
- [ ] 50%+ of test players play sparring weekly
- [ ] Every active test alliance runs at least one AvA match per week

---

## 12. Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Real-time netcode is the hardest thing in this project | 🔴 | 1v1 vertical slice first; scale format size only after it's solid |
| Server CPU at 20 Hz × many matches | 🟠 | Fixed-point sim is cheap; cap concurrent matches; queue overflow |
| Mobile latency ruins the feel | 🟠 | Client prediction + interpolation; forgiving hit windows; 20 Hz is enough for squad-level |
| Normalization feels arbitrary to whales | 🟡 | Raw mode available; no power rewards from normalized play; communicate the intent clearly |
| Low AvA participation | 🟡 | Scheduled slots + alliance gifts on victory + voice + spectating |
| Arena splits attention from the 4X game | 🟡 | Arena is an Arms Race phase and feeds Valor into the main economy — it's connected, not parallel |
| Determinism divergence between client and server | 🔴 | Fixed-point only, shared code, 1,000-fixture CI test |

---

## Next
- `16-netcode-realtime.md` — how §5.2 is actually built
- `10-FEATURE-voice-chat.md` ★ — the other half of this
- `checklists/phase-5-arena.md`
