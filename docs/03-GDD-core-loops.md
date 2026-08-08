# 03 — GDD: Core Loops

> Loops are the skeleton of the game. Everything else is muscle hung on these bones.
> Read this before any other GDD document.

---

## 1. The loop hierarchy

```
LIFETIME LOOP        (months)   — seasons, alliance identity, server politics
   └─ WEEKLY LOOP    (7 days)   — Alliance Duel, AvA arena, tech milestones
       └─ DAILY LOOP (24 h)     — Arms Race, login, missions, sparring
           └─ SESSION LOOP (5–20 min) — collect, spend, dispatch, fight
               └─ MOMENT LOOP (5–60 s) — a runner stage, a battle, a gate choice
```

Each loop feeds the one above. A healthy game has all five closed. **Most failed games in this genre have a strong moment loop and a broken weekly loop** — which is why they lose players in week two.

---

## 2. MOMENT LOOP — 5 to 60 seconds

The smallest unit of fun. Must be satisfying with **zero context**.

### 2.1 Runner moment
```
approach gates → read the two options → choose → squad changes size
    → immediate visual + audio confirmation → enemies appear → auto-fire
    → enemies pop → repeat
```
**Feel target:** every 3–5 seconds something visibly changes about your squad.
**Failure state:** if 8 seconds pass with no change, the moment loop is dead. Add a gate.

### 2.2 Collection moment
```
see a glowing resource icon → tap → number flies to HUD → HUD counts up
    → satisfying chime → icon resets with a timer
```
**Feel target:** tapping must feel *physically* rewarding. This is why the juice spec (`19`) is not optional.

### 2.3 Battle moment (arena)
```
read the field → issue an order → units respond within 100 ms
    → clash → damage numbers → a unit dies → tide visibly shifts
```
**Feel target:** the player must always be able to name *why* they are winning or losing.

---

## 3. SESSION LOOP — 5 to 20 minutes

The shape of one app open. **Design target: a satisfying session in 4 minutes, an engaging one in 15.**

```
┌─────────────────────────────────────────────────────────────┐
│ 1. OPEN                                                     │
│    → "While you were away" offline earnings popup           │
│    → red dots on everything claimable                       │
│    ↓                                                         │
│ 2. HARVEST  (60–90 s)                                       │
│    → collect resources, claim completed builds/research     │
│    → claim daily login, claim finished marches               │
│    ↓                                                         │
│ 3. INVEST  (60–120 s)                                       │
│    → start new builds, research, troop training              │
│    → all queues full = the goal state                        │
│    ↓                                                         │
│ 4. DISPATCH  (60–120 s)                                     │
│    → send marches to nodes / zombies                         │
│    → all march queues busy = the goal state                  │
│    ↓                                                         │
│ 5. ENGAGE  (variable, the "fun" part)                       │
│    → runner stages / arena sparring / AvA / rally / events   │
│    → this is the part that isn't a chore                     │
│    ↓                                                         │
│ 6. SOCIALISE  (variable)                                    │
│    → alliance chat, voice, help requests, gifts              │
│    ↓                                                         │
│ 7. CLOSE                                                     │
│    → every queue running, a reason to come back              │
└─────────────────────────────────────────────────────────────┘
```

### The critical design rule
**A player must never close the app with an empty queue.**

Empty queue = no reason to return = churn. Therefore:
- The UI actively surfaces empty queues with an unmissable red badge
- The "close" state is designed: before backgrounding, if any queue is idle, show a gentle prompt
- Offline income continues regardless, so returning is always rewarded

### Steps 1–4 are chores
Be honest about this. Steps 1–4 are *maintenance*, not fun. They exist to create investment and habit. **Step 5 is the fun.** If the ratio of chore to fun gets worse than 50/50, players quit. Two protections:
- **One-tap collect-all** for step 2 (unlocked early, not sold)
- Steps 3–4 have sensible defaults so a rushed player can do them in 20 seconds

---

## 4. DAILY LOOP — 24 hours

What makes a player open the app **more than once a day**. This is the difference between D7 8% and D7 20%.

| Time | Hook | Mechanism |
|---|---|---|
| **Morning** | Offline earnings cap reached (3 h) | Push notification: "Your generators are full" |
| **Midday** | Arms Race phase change | Push: "Construction phase starts in 15 min" |
| **Afternoon** | Builds/research complete | Push: "HQ upgrade finished" |
| **Evening** | **Prime time**: alliance active, voice busy, AvA scheduled, events peak | Social pull, not a notification |
| **Night** | Daily reset approaching, unclaimed missions | Push: "3 daily missions unclaimed — 2 h left" |

### Daily systems
- **Daily login ladder** — 7-day cycle, day 7 is a big reward, resets. Cheap, effective.
- **Daily missions** — ~8 tasks (collect ×N, train ×N, fight ×N, help alliance ×N) → a chest at mission-point thresholds
- **Arms Race** — 6 rotating 4-hour phases, each rewarding a different kind of spending. **The appointment mechanic.**
- **Free daily pulls / free shop items** — a free gacha pull daily is remarkably effective at driving opens
- **Alliance help** — helping others costs nothing and gives you currency, so there is always a free action available
- **★ Arena sparring dailies** — first 3 sparring matches each day give bonus rewards

### The Arms Race is the load-bearing wall
Six phases × 4 hours. Each phase scores a different activity:

| Phase | Scores |
|---|---|
| Construction | Resources spent on building |
| Research | Resources spent on tech |
| Training | Troops trained |
| Missions | Radar/bounty missions completed |
| Heroes | Hero EXP/shards consumed |
| **★ Arena** *(our addition)* | Arena matches played + won |

**Why it works:** it teaches players to **hoard, then dump on schedule**. A player who has saved 2M food and is waiting for the Construction phase at 16:00 **will open the app at 16:00**. That is a manufactured appointment, and appointments are the foundation of habit.

**★ Our addition:** the Arena phase gives F2P players a phase they can actually win — every other phase rewards spending, and arena rewards *playing*. This is a meaningful fairness improvement over the reference game.

---

## 5. WEEKLY LOOP — 7 days

What makes a player still be here in week 3. **This is the loop most projects forget, and it is why they die.**

```
Mon ─ Alliance Duel Day 1 (Construction)   + AvA challenges open
Tue ─ Alliance Duel Day 2 (Research)
Wed ─ Alliance Duel Day 3 (Training)       + ★ AvA scheduled matches
Thu ─ Alliance Duel Day 4 (Heroes)
Fri ─ Alliance Duel Day 5 (Arena) ★        + ★ AvA bracket tournament
Sat ─ Alliance Duel Day 6 (Total power)    + Crazy Joe
Sun ─ Rewards, rankings, ★ sparring ladder reset, rest day
```

### Weekly systems
- **Alliance Duel** — same scoring as Arms Race but pooled per alliance and ranked against rival alliances. Converts individual effort into **team obligation**, which is a far stronger motivator than personal reward.
- **Crazy Joe** — cooperative NPC wave defense. High participation, zero risk.
- **★ AvA arena** — scheduled alliance-vs-alliance battles. **The weekly appointment with the highest emotional stakes.**
- **★ Sparring ladder** — intra-alliance ELO, resets Sunday, "Alliance Champion" title awarded.
- **Weekly missions** — larger versions of dailies with a substantial chest.
- **Alliance shop rotation** — new stock weekly.

### Why the weekly loop is where retention actually lives
Daily loops create *habit*. Weekly loops create *identity and obligation*. A player who has committed to their alliance's Friday AvA match has made a **social promise**, and people keep social promises far more reliably than they keep personal goals.

**★ Design note:** voice chat multiplies this enormously. A player who *hears* their alliance planning Friday's match is dramatically more likely to show up than one who read it in a text channel. This is precisely why your two features reinforce each other — voice makes the weekly loop emotionally real.

---

## 6. LIFETIME LOOP — months

What makes a player still be here in month 6.

| Driver | Mechanism |
|---|---|
| **Season cycle** (~8 weeks) | New battle pass, new heroes, new event, leaderboard reset |
| **Alliance identity** | "I'm in [Alliance]" becomes part of how the player sees themselves |
| **Server politics** | Capitol Clash → President/Ministers → coalitions, rivalries, betrayals |
| **Rank progression** | HQ 30 endgame, tech tree completion, hero collections |
| **★ Arena legacy** | Seasonal AvA leaderboard, permanent alliance war record, champion history |
| **Friendship** | ★ The real one. People stay for people. |

### The endgame content is other players
This is the most important economic insight in the genre: once a player is at HQ 30 with maxed tech, **you cannot produce content fast enough to satisfy them**. But rival alliances, server politics, and arena rivalries are content that *the players generate for each other*, infinitely, for free.

So the lifetime loop is not "add more levels". It is:
1. Give players tools to compete with each other (arenas, war, leaderboards)
2. Give players tools to organise (alliances, ranks, ★ voice)
3. Give players stakes to fight over (Capitol, titles, territory, ★ war records)
4. Then get out of the way

---

## 7. Progression pacing map

The intended experience for a typical engaged F2P player. Used for tuning in `12-economy-balance-model.md`.

| Time played | HQ | Unlocked | Emotional beat |
|---|---|---|---|
| 0–2 min | — | Runner stage 1–3 | "Oh, this is fun" |
| 2–5 min | 1–2 | Base appears, first build | "There's more here" |
| 5–15 min | 3–4 | Troops, first hero, missions | "I'm getting stronger" |
| 15–30 min | 5 | **World map, alliances** | "There are other people" |
| 30–60 min | 6–7 | PvP, marches, ★ voice chat | "I have a team" |
| 1–3 h | 8–10 | **★ Arena sparring**, gacha, tech | "I can compete" |
| Day 2–3 | 11–13 | Arms Race, events, first real wall | "I need to plan" |
| Week 1 | 14–16 | Alliance Duel, ★ AvA arena | "My alliance needs me" |
| Week 2–4 | 17–21 | Rallies, territory, Crazy Joe | "We're at war" |
| Month 2–3 | 22–26 | Capitol Clash, seasons | "I'm somebody here" |
| Month 4+ | 27–30 | Endgame, leaderboards, legacy | "This is my community" |

### Two deliberate pacing decisions

**1. Alliances at 15 minutes, not 2 hours.**
The reference game gates alliances later. We pull it forward because our differentiator is social, and a player who joins an alliance in session one retains far better. The risk (overwhelming a new player) is managed by keeping alliance UI minimal until HQ 6.

**2. Arena sparring at HQ 8, ~1–3 hours in.**
Early enough that the player still has momentum, late enough that they have troops and a hero worth fighting with. Sparring is the first system where a F2P player can beat a spender, and that moment matters.

---

## 8. Loop health metrics

Each loop has a measurable health indicator. If one goes red, that loop is broken and must be fixed before adding anything new.

| Loop | Metric | Healthy | Broken |
|---|---|---|---|
| Moment | Runner stage completion rate | > 70% | < 50% |
| Session | Median session length | 6–15 min | < 3 min |
| Session | Sessions per DAU per day | > 3 | < 1.5 |
| Daily | D1 retention | > 35% | < 25% |
| Daily | Arms Race participation | > 60% | < 30% |
| Weekly | D7 retention | > 15% | < 8% |
| Weekly | Alliance Duel participation | > 50% | < 25% |
| Weekly | ★ AvA matches per alliance | ≥ 1 | 0 |
| Lifetime | D30 retention | > 6% | < 3% |
| Lifetime | % of DAU in an alliance | > 70% | < 40% |
| Lifetime | ★ % using voice weekly | > 40% | < 15% |

Full definitions and instrumentation in `21-analytics-kpis.md`.

---

## 9. Anti-patterns we are explicitly avoiding

| Anti-pattern | Why it kills games | Our counter |
|---|---|---|
| Chore-heavy sessions | Maintenance overwhelms fun | One-tap collect-all; chore budget ≤ 50% of session |
| Empty-queue exit | No reason to return | Active empty-queue warnings before backgrounding |
| Pay-gated progress walls | F2P players quit at the wall | Every wall passable free, just slower |
| Dead endgame | Nothing to do at max level | Player-generated content: arenas, politics, war |
| Silent alliances | Text chat is low-bandwidth and dies | ★ Voice chat makes alliances audibly alive |
| Nothing to do between events | Dead air kills habit | ★ Arena sparring is always available, always rewarding |
| Losing your army | Catastrophic loss = uninstall | Wounded-not-dead, zero-loss sparring |

---

## Next
- `04-GDD-base-buildings.md` — the base layer in full
- `06-GDD-runner-minigame.md` — the moment loop we build first
- `12-economy-balance-model.md` — the maths behind the pacing map
