# 09 — GDD: Events & Live-Ops Calendar

> After HQ ~15, players do not log in to build. They log in because **an event is running**.
> The calendar *is* the retention product. Phase 6.

---

## 1. The hard architectural requirement

**An event must be launchable with zero client builds.** If shipping an event requires an app store release, live-ops is dead — you get one event every two weeks instead of three per week.

This means:
- Event definitions are **server data** (JSON), not client code
- Event UI is **server-driven**: the server sends a layout descriptor (banner, tabs, milestone list, reward grid) and the client renders it from a small set of generic components
- Reward tables, scoring rules, schedules, and copy all live server-side
- The client ships **~8 generic event UI templates**; any new event maps to one of them

This is the Phase 6 → 7 gate criterion. Full technical design in `14 §7`.

---

## 2. Event framework model

```
EventDefinition {
  id, type, template, startAt, endAt, timezoneMode,
  phases[]      { id, startOffset, duration, scoringRules[] }
  scoringRules[]{ trigger, pointsFormula, dailyCap }
  milestones[]  { points, rewards[] }
  rankRewards[] { rankFrom, rankTo, rewards[] }
  requirements  { minHq, minAllianceSize, ... }
  uiCopy        { localised strings per language }
}
```

**Scoring triggers** are the generic hook. The game emits domain events (`ResourcesSpentOnBuilding`, `TroopTrained`, `ZombieKilled`, `ArenaMatchWon`, …) and the event engine subscribes to them. Adding a new event means writing a JSON row, not new code, as long as it scores on existing triggers.

---

## 3. Launch calendar

### Daily — Arms Race
6 rotating phases × 4 h. Each scores one activity.

| Phase | Scores |
|---|---|
| Construction | Resources spent on buildings |
| Research | Resources spent on tech |
| Training | Troops trained/promoted |
| Missions | Bounty/radar missions completed |
| Heroes | Hero EXP + shards consumed |
| **★ Arena** | Arena matches played + won |

Milestone rewards per phase + a daily rank leaderboard.

**Why it works:** it teaches players to hoard resources and **dump on schedule**. A player waiting for the 16:00 Construction phase will open the app at 16:00. That is a manufactured appointment, and appointments are the foundation of habit.

**★ The Arena phase is our addition and it matters:** every other phase rewards *spending*. The Arena phase rewards *playing*. It is the one phase a F2P player can top, which meaningfully improves the fairness of the whole event.

### Weekly — Alliance Duel
6 days, each mirroring an Arms Race phase, but points pool **per alliance** and alliances are ranked against each other.

**Why it works:** converts individual spending into **team obligation**. Players who would not spend for themselves will spend for their alliance. It is the strongest motivational pressure in the game, and it costs almost nothing to build once Arms Race exists — the scoring rules are literally the same rows with a different aggregation key.

### Weekly — Crazy Joe
NPC waves assault member bases in escalating rounds; the alliance defends collectively.
- 10 rounds, escalating difficulty
- Losses are **fully healed free** at event end — **zero real risk**
- Alliance-pooled score → shared rewards

**Why it works:** cooperative excitement with no downside. Non-competitive players get a way to matter. Participation is typically the highest of any event.

### Weekly + on-demand — ★ AvA Arena
Your feature. Fills the alliance-vs-alliance slot. Full spec in `11-FEATURE-arena-rooms.md`.
- Scheduled Wednesday/Friday matches
- On-demand challenges any time
- Weekly bracket tournament (8 or 16 alliances)
- War Points → alliance shop + seasonal leaderboard

### Cyclic (14 days) — Capitol Clash
Server-wide fight for the Capitol at map centre.
- Winning alliance's R5 becomes **President**, appoints **Ministers**
- Titles grant real buffs and the power to grant buffs to others
- Holding the Capitol yields continuous alliance income

**Why it works:** it creates **server politics** — coalitions, negotiations, betrayals. This is the deepest retention hook available because the content is *other players*, which is infinitely renewable and free to produce.

### Always-on
| System | Function |
|---|---|
| Daily login ladder | 7-day cycle, day 7 is big |
| Daily missions | ~8 tasks → mission-point chests |
| Weekly missions | Larger, bigger chest |
| 8-day server-opening ladder | New-state FTUE, front-loads progression |
| Bounty tasks | Refreshing small objectives |
| Rally bosses | Continuous alliance PvE |
| Lucky wheel | Gambling loop, free daily spin |
| Season pass | 8-week cycle, free + paid tracks |

### Deferred
**Desert Storm** — the reference game's flagship AvA battlefield. **Cut**, because ★ AvA Arena rooms cover the same need with better netcode, faster matches, and voice integration. Revisit only if AvA metrics show demand for a slower, larger-scale format.

---

## 4. Weekly schedule

```
Mon  Alliance Duel D1 (Construction)   · AvA challenge window opens
Tue  Alliance Duel D2 (Research)
Wed  Alliance Duel D3 (Training)       · ★ AvA scheduled matches
Thu  Alliance Duel D4 (Heroes)
Fri  Alliance Duel D5 (★ Arena)        · ★ AvA bracket tournament
Sat  Alliance Duel D6 (Total Power)    · Crazy Joe
Sun  Rewards & rankings                · ★ Sparring ladder reset
```
Arms Race runs daily underneath all of it.

---

## 5. Season structure (8 weeks)

| Week | Beat |
|---|---|
| 1 | Season opens, new pass, new hero banner, leaderboards reset |
| 2–3 | Ramp; themed limited event |
| 4 | Mid-season: second hero, Capitol Clash peak |
| 5–6 | Competitive peak; AvA bracket finals |
| 7 | Final push; catch-up rewards for latecomers |
| 8 | Wind-down, rewards, teaser for next season |

The week-7 catch-up rewards matter: without them, anyone who joins mid-season feels locked out and churns.

---

## 6. Timezone handling

Non-obvious and important:
- Arms Race phases run on **server time** (UTC+0), so all players share the same schedule and can coordinate
- Daily reset at **02:00 UTC**
- Daily missions/login use **server day**, not device day, to prevent timezone-hopping exploits
- ★ AvA scheduled matches let the challenger pick from fixed UTC slots; the client displays them in local time

Displaying local time while scheduling in UTC is essential for global alliances — and it is the kind of thing that is painful to retrofit.

---

## 7. Admin panel (Phase 6)

Self-hosted, free, behind auth + IP allowlist:
- Create/edit/schedule events without deploys
- Player lookup: state, purchases, ban status, grant compensation
- Alliance lookup and moderation
- ★ Voice moderation queue (`10 §6`)
- ★ Arena match log + replay viewer
- Balance table hot-reload
- Announcement broadcast

**Security note:** the admin panel can modify player state and grant currency, so it is a high-value target. Requirements: strong auth (TOTP), IP allowlist, every action written to an append-only audit log, and no public internet exposure — access via SSH tunnel or a private network only. Detailed in `20-security-anticheat.md`.

---

## 8. Push notifications

| Trigger | Copy style |
|---|---|
| Offline income capped | "Your generators are full" |
| Build/research complete | "HQ upgrade finished" |
| Arms Race phase change (opt-in) | "Construction phase starts in 15 min" |
| Under attack | "Your base is under attack!" |
| ★ AvA match starting | "Your alliance war starts in 10 minutes" |
| ★ Arena challenge received | "[Name] challenged you to a duel" |
| Alliance help requested | Batched, max 1/hour |
| Unclaimed daily missions | Evening, once |

**Hard cap: 4 pushes per day.** Per-category opt-out. Over-notifying is the fastest route to an uninstall, and most games in this genre get this badly wrong.

---

## 9. Data files

| File | Contents |
|---|---|
| `events/*.json` | Event definitions (server-side, hot-reloadable) |
| `event_templates.csv` | templateId → client UI component mapping |
| `scoring_triggers.csv` | triggerId, domain event, default formula |
| `season_config.json` | Season length, pass tiers, rewards |
| `push_templates.csv` | notificationId, category, localised copy |

---

## 10. Phase 6 acceptance criteria
- [ ] Event framework: definitions load from JSON, hot-reloadable
- [ ] Server-driven UI: 8 generic templates render any event
- [ ] Arms Race with 6 phases including ★ Arena
- [ ] Alliance Duel with per-alliance aggregation
- [ ] Crazy Joe with free post-event healing
- [ ] ★ AvA Arena integrated as an event
- [ ] Capitol Clash with President/Minister titles and buffs
- [ ] Season pass framework
- [ ] Admin panel with auth, audit log, no public exposure
- [ ] Push notifications with 4/day cap and per-category opt-out
- [ ] **✅ GATE: a brand-new event launched to a live client with no client build**

---

## Next
- `10-FEATURE-voice-chat.md` ★
- `11-FEATURE-arena-rooms.md` ★
- `13-monetization-iap.md`
