# 01 — Competitive Teardown: *Last War: Survival*

> **Purpose:** understand *why* the game works, not just what's in it. Every section ends with a **→ Zero Hour decision** so this doc drives implementation rather than sitting on a shelf.
>
> **Confidence notation:**
> - ✅ **Observed** — directly visible in normal play, widely documented
> - 🔶 **Inferred** — deduced from behaviour; structurally right, exact values unknown
> - ⚠️ **Estimated** — my modelling; treat numbers as starting points to tune
>
> **No assets, code, or data were extracted from the game.** This is black-box design analysis, which is standard, legal practice. See `22-legal-compliance.md`.

---

## 1. Executive summary

| | |
|---|---|
| **Title** | Last War: Survival (a.k.a. Last War: Survival Game) |
| **Developer** | FirstFun |
| **Released** | 2023 |
| **Platforms** | iOS, Android |
| **Genre** | 4X strategy / base-builder, with a hyper-casual runner front-end |
| **Setting** | Modern post-apocalyptic zombie survival |
| **Monetization** | F2P + IAP (packs, gacha, battle pass, VIP) |
| **Scale** | Top-grossing strategy title; hundreds of millions USD annually ⚠️ |

**The one-sentence insight:**
> Last War took the proven, decade-old *Rise of Kingdoms / State of Survival* 4X machine and bolted a **hyper-casual lane-runner** onto the front of it. The runner made the ads truthful, the tutorial frictionless, and the first session satisfying. That single structural decision is why it beat competitors who had equal or better 4X depth.

**Why this matters for you:** you cannot out-4X a 100-person studio with a 14-month budget. You *can* match and beat the front-end hook, and the front-end hook is what acquires players.

---

## 2. The three-layer architecture

The whole game is three loosely-coupled games sharing one economy. Understanding this is the key to scoping.

```
┌───────────────────────────────────────────────────────────────┐
│ LAYER 3 — SQUAD / RUNNER            (hyper-casual, 60–90 s)   │
│ Lane runner · gates · waves · boss                            │
│ Purpose: ACQUISITION + early retention + idle income engine    │
│ Session role: the "one more try" impulse                      │
└──────────────────────────┬────────────────────────────────────┘
                           │ feeds resources & progression
┌──────────────────────────▼────────────────────────────────────┐
│ LAYER 1 — BASE                       (mid-core, 2–5 min)      │
│ Isometric city · build · upgrade · train · research            │
│ Purpose: FREQUENCY (timers create appointments)                │
│ Session role: the "check in on my stuff" habit                 │
└──────────────────────────┬────────────────────────────────────┘
                           │ produces power & marches
┌──────────────────────────▼────────────────────────────────────┐
│ LAYER 2 — WORLD                      (hardcore, 10–60 min)    │
│ Shared grid · marches · PvP · alliances · events               │
│ Purpose: RETENTION + MONETIZATION (social obligation)          │
│ Session role: the "my alliance needs me" compulsion            │
└───────────────────────────────────────────────────────────────┘
```

**Design law observed:** each layer retains a *different* player. Layer 3 keeps the casual, Layer 1 keeps the collector, Layer 2 keeps the competitor. Removing any layer costs you a whole audience segment.

**→ Zero Hour decision:** build Layer 3 first and ship it standalone (Phase 1). Then Layer 1 (Phase 2). Then Layer 2 (Phase 4). Never build them simultaneously.

---

## 3. LAYER 3 — The lane-runner (the thing everyone underestimates)

### 3.1 What it is ✅
A squad of soldiers auto-runs forward down a corridor. The player drags left/right. The corridor is filled with:

- **Gates** — translucent panels with an operator: `+12`, `×2`, `−5`, `÷2`, or a weapon/type upgrade
- **Zombie waves** — trickling enemies that the squad auto-fires at
- **Obstacles** — barriers that shrink your squad if hit
- **Mid-boss** — a fat enemy mid-lane
- **Finale** — a wall or boss with a large HP bar; you win by out-damaging it

Runtime: **45–90 seconds**. No fail-state punishment beyond retrying.

### 3.2 Why it is strategically brilliant

| Reason | Mechanism |
|---|---|
| **The ads are honest** | The ad creative *is* the game. No bait-and-switch, so install→tutorial conversion is high and store reviews don't collapse |
| **Zero learning cost** | One input: drag. A player is competent in 3 seconds |
| **Instant dopamine** | `×2` gates create exponential visual escalation — 5 soldiers become 200 in 40 seconds |
| **Perfect tutorial** | Teaches nothing about 4X, but earns the player's investment before the 4X complexity arrives |
| **Idle engine host** | Stage progress = offline income rate. Gives the base layer something to *do* on day 1 |
| **Cheap content** | New stages are data rows, not art. Infinite scaling at near-zero cost |

### 3.3 Mechanics detail 🔶

**Gate operators** — the maths is deliberately unfair in the player's favour:
- `+N` additive, `×N` multiplicative, `−N` / `÷N` as punishing choices
- Gates are presented in **pairs** so the player makes a *choice*, not a reflex. This is the entire skill expression.
- Occasional gates swap weapon tier or unit type, adding a light strategic dimension

**Difficulty curve** ⚠️ my model:
```
EnemyHP(stage)   = H0 · g^stage          g ≈ 1.13 – 1.18
PlayerPower      = units × dmgPerUnit × upgradeMult
RequiredPower(s) ≈ EnemyHP(s) · k        k ≈ 1.1 safety factor
```
The curve is tuned so that around **every 4–6 stages** the player stalls. The stall is resolved by:
1. Spending idle income on permanent upgrades (free, slow), **or**
2. Watching a rewarded ad / spending premium (fast)

**That stall point is the monetization funnel.** It is not an accident and it is not a difficulty bug.

**Idle income** ✅:
```
OfflineRate = baseRate · f(highestStageCleared)
Accrual capped at 2–4 h offline (uncapped via VIP/premium)
```
Login → "While you were away…" popup → collect. This is the **single most important D1 retention mechanic in the entire game.** It creates a reason to reopen the app that costs the player nothing.

### 3.4 → Zero Hour decisions
- **Build this first, ship it standalone.** It is a complete product on its own.
- Gates always appear in pairs — preserve the choice
- Runtime target: 60 s ± 15 s
- Offline cap: 3 h free, 8 h with premium (a clean, honest upsell)
- Stage data in CSV, not code — you will retune this hundreds of times
- Deterministic given `(stageId, seed, upgrades)` so the server can validate any claimed result

---

## 4. LAYER 1 — Base & progression

### 4.1 Building roster ✅
Isometric 2.5D base. Buildings, grouped by function:

**Command**
| Building | Function |
|---|---|
| **Headquarters (HQ)** | The master gate. Every other building is capped at HQ level. Upgrading HQ is the central spine of all progression |
| Wall / Defenses | Base defense stat, turret slots |
| Alliance Center | Help requests, alliance features |
| Radar Station | Mission/task dispatch, intel |

**Economy**
| Building | Produces |
|---|---|
| Farm | Food |
| Iron Mine | Iron |
| Bank | Coin |
| Warehouse | **Protected** resource capacity (cannot be looted) |
| Trade Center | Resource exchange |

**Military**
| Building | Function |
|---|---|
| Armored Vehicle Factory | Trains Tank-type troops |
| Air Force Base | Trains Air-type troops |
| Missile Vehicle Factory | Trains Missile-type troops |
| Drill Ground | Troop capacity / training buffs |
| Hospital | Heals **wounded** troops |

**Growth**
| Building | Function |
|---|---|
| Tech Center / Research Lab | The tech tree |
| Hero Recruitment Center | Gacha + hero management |
| Drone Command | Parallel drone progression |
| Truck Depot | Supply/trade events |

### 4.2 The gating tree — the real engine ✅

```
HQ Level
 ├─ caps every building level
 ├─ unlocks new buildings
 ├─ unlocks tech tiers
 ├─ unlocks troop tiers
 └─ unlocks features (alliance war, events, arena)
```

Requirement chains look like: *HQ 12 requires Farm 11 + Iron Mine 11 + Tech Center 11*, so upgrading HQ forces broad, balanced investment. No single-track rushing.

### 4.3 Time as the product ✅

Build times grow geometrically:

| HQ level band | Typical upgrade time | ⚠️ estimated |
|---|---|---|
| 1–10 | seconds → minutes | |
| 11–17 | 30 min → 6 h | |
| 18–22 | 8 h → 1.5 days | |
| 23–27 | 2 → 5 days | |
| 28–30+ | 6 → 20+ days | |

```
BuildTime(L) = a · b^L        b ≈ 1.35 – 1.5
```

**This is the business.** The game is not selling resources; it is selling *time*. Everything else is scaffolding around the sale of time.

Time-reduction levers, in the order a player discovers them:
1. **Alliance Help** — each member click removes ~1% of remaining time, ~30 clicks max ✅
2. **Speedup items** — 1 m / 5 m / 1 h / 8 h / 24 h, earned and sold
3. **Research/tech** construction speed %
4. **VIP** construction speed %
5. **Second build queue** — a hard paywall/deep-tech unlock

**The Alliance Help mechanic is the most important social design in the genre.** It makes joining an alliance non-optional in *mechanical* terms, which drives every downstream social and monetization system. A player without an alliance progresses roughly 30% slower — so nobody stays alliance-less.

### 4.4 Resources ✅

| Resource | Role | Sink |
|---|---|---|
| **Food** | Bulk soft | Building, training, healing |
| **Iron** | Bulk soft | Building, research |
| **Coin** | Mid soft | Research, hero upgrades |
| **Diamonds** | 💎 Premium | Speedups, gacha, packs, revives |
| **Speedups** | The real currency | Time |
| Hero EXP | Progression | Hero levels |
| Hero Shards | Gacha dupe currency | Hero stars |
| Valor Badges | Alliance/event currency | Alliance shop |
| Tech Points | Research | Tech tree |
| Drone Parts / Data | Drone track | Drone levels/skills |
| VIP Points | Status | VIP ladder |

**Observation:** ~11 currencies. This is intentional. Multiple currencies let you gate content precisely, run targeted events ("earn Valor Badges this week"), and obscure real exchange rates so players cannot easily compute value-for-money.

### 4.5 Tech tree ✅
Three branches, hundreds of nodes, each a small percentage:
- **Economy** — production, capacity, gather speed, construction speed
- **Battle** — attack, defense, HP, per-troop-type bonuses
- **Growth** — training speed, healing, march capacity, queues

Late nodes cost days and give +1%. This looks absurd but is the **long-tail retention engine**: a level-40 player always has something to spend on, and compounding % on top of large base numbers is meaningful.

### 4.6 → Zero Hour decisions
- HQ-gated tree: **keep exactly**, it is proven
- Alliance Help: **keep exactly**, it is the social keystone
- Cut to **12 buildings** for v1 (from ~18). Drone track deferred to Phase 6+
- Currencies: **6 at launch** (Food, Iron, Coin, Diamonds, Speedups, Valor). Add more only with a live-ops reason
- Time curve: keep geometric but **gentler than Last War** — a smaller game cannot afford 20-day timers. Target max ~4 days at endgame
- Tech tree: 3 branches, ~120 nodes at launch, extensible by data

---

## 5. LAYER 1b — Troops & Heroes

### 5.1 Troop types and the counter triangle ✅

Three types: **Tank**, **Air**, **Missile**, in a rock-paper-scissors relationship with roughly **+25–30%** damage on advantage 🔶.

> ⚠️ **Verify in-game before locking.** Community sources disagree on orientation, and it's a pure tuning knob anyway. Zero Hour ships it as a data table:
> `Missile ▶ Tank ▶ Air ▶ Missile`

Each type has ~10 tiers. Higher tiers are **promoted** from lower tiers (not retrained from scratch) — an important retention detail, because it means a player's early investment is never wasted.

### 5.2 Heroes ✅ — the primary paywall

- Rarities: **SR → SSR → UR** (plus limited/seasonal exclusives)
- Every hero is bound to **one troop type**
- Progression axes:
  1. **Level** (Hero EXP) — cheap, steady
  2. **Stars** (duplicate shards) — the gacha sink
  3. **Skills** (4 per hero) — skill books/materials
  4. **Exclusive Gear** — late-game, expensive, large multipliers
  5. **Awakening** — endgame, huge multipliers

- **Squad = 3 heroes.** The **leader** determines the squad's troop type. Squads are assigned to marches.

**Why this design monetizes so well:** five parallel progression axes per hero × dozens of heroes = an effectively bottomless pit. A whale can always spend more, and a F2P player always sees the gap.

### 5.3 Gacha ✅
- Premium recruitment vs. basic recruitment
- 10-pull discounts, guaranteed-rarity pity counters
- Limited-time banners for new/seasonal heroes
- Duplicates convert to shards (never a dead pull)

### 5.4 Combat model 🔶
Battles are **resolved, not played** — you dispatch and receive a report. The resolver considers:

```
EffectiveAtk = Σ(troopAtk · tierMult) · (1 + heroAtk% + techAtk% + vipAtk%)
                · counterTriangleMult · terrainMult
EffectiveDef = analogous
Rounds resolve until one side breaks; casualties split
   dead / wounded by a threshold rule
```

**The wounded-vs-dead rule is the most important retention mechanic in PvP.** Below a damage threshold, losing troops are only **wounded** and go to the Hospital, healable for a fraction of their build cost. A player who loses a battle does not lose their army. If they did, they would quit. Every successful game in this genre has some version of this rule.

### 5.5 → Zero Hour decisions
- 3 troop types × **8 tiers** (not 10)
- Counter triangle in a data table, tunable live
- Heroes: launch with **18 heroes** across 3 rarities
- Progression axes: **Level + Stars + Skills** at launch. Gear and Awakening reserved as the Phase 6+ content pipeline
- Gacha: hard pity at **60 pulls** for the top rarity, **rates published in-game** (legally required in several territories, and it builds trust)
- Combat: fully deterministic in `shared/Sim`, fixed-point maths, replayable from seed
- **Wounded/dead threshold: keep. Non-negotiable.**

---

## 6. LAYER 2 — World map & social

### 6.1 The State ✅
- A shared grid world ("State" / server), roughly **1024×1024 tiles** ⚠️
- **10,000–40,000 players** per state ⚠️
- States open on a schedule; new states get a fresh land-rush and an 8-day opening event ladder
- Terrain zones by level; higher-level content further from spawn
- Teleport items to relocate (a purchasable convenience with real strategic weight)

### 6.2 Marches ✅
- Dispatch a squad to a map target
- **March queue count is limited** (gated by tech and VIP) — the core throughput bottleneck
- Travel time by distance; march-speed buffs are highly valued
- **Rally** = alliance members combine marches against a strong target
- Recall available mid-flight

### 6.3 Map content ✅
| Target | Yields |
|---|---|
| Food / Iron nodes (L1–L10) | Bulk resources over time |
| Zombie units (L1–L30) | Hero EXP, chests, event points, stamina sink |
| Elite/boss zombies | Rare materials, rally targets |
| Gold Mines / Oil Rigs | Contested, alliance-held, continuous yield |
| Enemy bases | Loot + rank |
| Alliance turrets / flags | Territory control |

**Stamina** gates zombie hunting — a classic session-length limiter with a premium refill.

### 6.4 PvP ✅
- **Scout** first (costs an item, returns intel)
- **Attack** → resolved battle → loot unprotected resources
- **Warehouse** protects a portion — this cap is what stops PvP from being ruinous
- **Shields** (peace shields) block attacks; given generously to new players, sold thereafter
- Losing = wounded troops + some loot, **not** account destruction

### 6.5 Alliances ✅ — where the game actually lives

| Feature | Effect |
|---|---|
| Up to **100 members** | Big enough for politics, small enough for identity |
| Ranks **R1–R5** | R5 leader, R4 officers with real powers |
| **Alliance Help** | ~1%/click construction time reduction (the keystone) |
| **Alliance Tech** | Members donate → alliance-wide permanent buffs |
| **Alliance Gifts** | **When any member buys a pack, everyone gets a gift.** |
| Alliance Shop | Spend Valor Badges |
| Territory / flags / turrets | Map control, buffs inside territory |
| Rally | Combined attacks |
| Alliance Chat | With **auto-translate** — essential for global servers |

> **The Alliance Gift mechanic deserves its own paragraph.** When a whale spends, 99 other players receive free rewards and a notification naming the buyer. This converts individual spending into *social status* and makes the other 99 players actively grateful for and encouraging of whale spending. It is the single most effective monetization-through-social-design mechanic in the genre. Implement it.

### 6.6 → Zero Hour decisions
- Map: **512×512** for v1, ~5,000 players/state. Smaller map = denser, more interesting conflict, and far cheaper to host
- March queues: 2 free, up to 5 via tech/VIP
- **Warehouse protection: keep.** **Shields: keep.** **Wounded rule: keep.** These three are what keep losers playing
- Alliance cap: **60 members** for v1 (better cohesion at low DAU; raise later)
- **Alliance Help + Alliance Tech + Alliance Gifts: all three at launch.** Non-negotiable
- Chat auto-translate via self-hosted LibreTranslate ($0)
- ★ **Your additions land here:** voice chat (`10`) and arena rooms (`11`) both attach to the alliance layer

---

## 7. The live-ops calendar — this *is* the game after HQ 20

Past the early game, players do not log in to build. They log in because **an event is running**. The calendar is the retention product.

### 7.1 Daily: Arms Race ✅
Six rotating phases through the day (typically 4 h each):

| Phase | Scoring action |
|---|---|
| Construction | Spend on building/upgrades |
| Research | Spend on tech |
| Training | Train troops |
| Radar / Drone | Complete missions, drone activity |
| Hero | Level/upgrade heroes |
| (rotates) | |

**Effect:** trains players to **hoard resources and dump them at a scheduled time**. This is a masterclass in appointment mechanics — it converts an idle game into a scheduled one, and scheduled play is sticky play.

### 7.2 Weekly: Alliance Duel ✅
Six days, each mirroring an Arms Race phase, but points are **pooled per alliance** and alliances are ranked against each other.

**Effect:** converts individual spending into **social obligation**. "Our alliance is 400 points behind, everyone train troops now." A player who would not spend for themselves will spend for their team. This is the strongest monetization pressure in the entire game and it costs nothing to build once the Arms Race framework exists.

### 7.3 Weekly: Crazy Joe ✅
NPC waves assault member bases in escalating rounds; the alliance coordinates defense.

**Effect:** cooperative PvE excitement with **zero risk of real loss**. Gives non-competitive players a way to matter. Very high participation.

### 7.4 Bi-weekly: Desert Storm ✅
Two matched alliances enter a **separate battlefield**, capture and hold facilities, score over a fixed window.

**Effect:** the flagship alliance-vs-alliance event. Scheduled, high-stakes, requires coordination — which is precisely why voice chat (your feature) is such a natural fit.

### 7.5 Cyclic: Capitol Clash ✅
Server-wide competition for the **Capitol**. The winning alliance's leader becomes **President** and appoints **Ministers**, each title granting real gameplay buffs and the power to distribute buffs to others.

**Effect:** creates **server politics**. Alliances negotiate, betray, form coalitions. This is the deepest retention hook in the genre because the content is *other players*, which is infinitely renewable and free to produce.

### 7.6 Always-on ✅
Daily login ladder · 8-day new-server ladder · Bounty tasks · Secret tasks · Truck rescue · Marshal boss · Lucky wheel · Limited bundles · Season pass

### 7.7 → Zero Hour decisions
Launch calendar, in build priority order:
1. **Arms Race** (daily) — highest value-to-effort ratio in the entire game
2. **Alliance Duel** (weekly) — reuses Arms Race scoring wholesale
3. **Crazy Joe** (weekly) — cheap, high participation
4. **★ Arena Rooms** (your feature) — fills the AvA slot with something *better* than Desert Storm
5. **Capitol Clash** (cyclic) — Phase 6, needs a mature player base to matter
6. Desert Storm — **deferred**, arena rooms cover this need

**Mandatory architecture requirement:** the event framework must be **data-driven with server-driven UI**. If launching an event requires an app store update, live-ops is dead. This is a Phase 6 gate criterion.

---

## 8. Monetization anatomy

### 8.1 The offer ladder ✅
| Offer | Price band | Function |
|---|---|---|
| Starter Pack | $0.99–1.99 | **Breaks the payment barrier.** Absurd value on purpose |
| Growth Fund | $4.99–9.99 | Pay once, receive drip rewards as you level → creates a return obligation |
| Monthly Card (×2 tiers) | $4.99 + $9.99 | Daily diamonds → **daily login contract** |
| Season / Battle Pass | $9.99–19.99 | Free + paid tracks; the paid track makes the free track feel incomplete |
| VIP ladder (1–20) | cumulative | Permanent % buffs + extra queues; ratchets and never resets |
| Hero banners | $9.99–99.99 | The deep well. Pity makes big spends feel rational |
| Contextual offers | $4.99–49.99 | **Triggered by game state** — you just got attacked → shield bundle |
| Resource/speedup bundles | $9.99–99.99 | Whale throughput |
| Lucky wheel | $0.99/spin | Gambling loop |

### 8.2 The revenue shape ⚠️ (industry standard for the genre)
- ~**2%** of players ever pay
- ~**60–70%** of revenue from the top **0.1%** ("whales")
- ARPDAU roughly **$0.15–0.50**

### 8.3 The uncomfortable truth you must design around
**The 98% who never pay are not failed customers — they are the product.** Whales pay to dominate, and domination requires a population to dominate. So the design must keep F2P players *present, active, and reasonably happy*:

- Wounded-not-dead troops → losing does not end your account
- Warehouse protection → you cannot be stripped bare
- Shields → you can opt out of pain temporarily
- Alliance Gifts → whale spending **directly benefits** F2P players
- Crazy Joe / PvE events → non-competitive players still progress

Every one of those mechanics exists to retain non-payers. Remove them and your whales run out of people to fight, then leave too.

### 8.4 → Zero Hour decisions
- Ship the ladder: Starter → Growth Fund → Monthly Card → Battle Pass → VIP → banners
- **Publish gacha rates.** Legally required in some markets, and it builds the trust a small studio needs
- Contextual offers: **yes**, but never exploitative-by-design (no "you're about to lose everything" panic-selling)
- **Alliance Gifts: mandatory at launch**
- No pay-to-win that cannot be reached free — every advantage must be earnable, just slower. This is both an ethics position and a retention strategy

---

## 9. UX / UI teardown

### 9.1 Signature interactions ✅
1. **Seamless base ⇄ world zoom** — pinch out from your base and the camera continuously flies up into the world map. No loading screen, no mode switch. It makes two separate games feel like one place. **This single interaction does more for cohesion than any other UI decision in the game.**
2. **Red-dot everywhere** — a formal notification tree, every collectible/claimable/upgradeable surfaces a badge that propagates up to the parent button
3. **Reward juice** — chest shake → burst → particle spray → items fly out → delayed count-up on the currency HUD. Every single reward gets this treatment
4. **Event Center hub** — one button aggregating every active event with timers
5. **One-thumb portrait** — every primary action is reachable in the bottom third of the screen

### 9.2 Screen inventory 🔶
~40–60 distinct full-screen UIs. This is where the real content budget goes in this genre — not art, **UI**.

### 9.3 Art direction ✅
- Low-poly stylised 3D on a fixed isometric camera
- Bright, high-saturation, readable-at-thumbnail-size (critical for ad creatives)
- Heavy 2D UI overlay: metal panels, rivets, warning stripes, military stencil type
- Zombies are cartoonish, not horror — keeps the age rating broad

### 9.4 → Zero Hour decisions
- **Seamless zoom transition: must-have.** Budget real time for it in Phase 2
- Red-dot system: build it as a **formal tree from day one**. Retrofitting ad-hoc booleans is a nightmare. See `19`
- Juice: build a reusable `RewardPresenter` so every reward automatically gets the full treatment
- Art: single consistent CC0 low-poly style + strong lighting/post-processing. See `18`
- Portrait-only for v1

---

## 10. Technical inference 🔶

What the game's behaviour tells us about its architecture:

| Observation | Implication |
|---|---|
| Timers continue correctly with the app closed, and resist device clock changes | Server-authoritative time, not client |
| Battle reports arrive as detailed after-the-fact summaries | Server-side resolution, client renders a replay |
| World map loads in patches as you scroll | Chunked spatial queries, not a full-map download |
| Balance changes appear without an app update | Server-served config tables |
| New events appear without an app update | **Server-driven UI** |
| 10k+ players share a map with no visible tick lag | **Lazy state evaluation** — state resolved on read, not globally ticked |
| Instant response on resource collection | Client-side prediction with server reconciliation |

**The lazy-evaluation insight is the most important one for you.** A naive implementation ticks every base every second — that is 40,000 updates/sec and needs a server farm. The correct implementation stores `lastTouchedAt` and computes elapsed production only when someone reads that base. **This is what makes a free 4-core VM able to host a whole game world.**

**→ Zero Hour decisions:** all seven rows above are adopted as architecture requirements. See `14-tech-architecture.md`.

---

## 11. What Last War does that we will *not* copy

| Thing | Why not |
|---|---|
| 20-day endgame timers | Only tolerable with a massive population and huge UA spend. Cap at ~4 days |
| ~11 currencies | Confusing; obscures value deliberately. Launch with 6 |
| ~18 base buildings | Content cost we cannot afford. Launch with 12 |
| Hidden gacha rates | Publish them. Trust is a small studio's only real asset |
| Panic-triggered offers | Predatory. Contextual offers yes, manufactured panic no |
| Ad creatives that misrepresent | Our runner *is* the game, so we never need to lie |
| Drone track at launch | A whole parallel system. Defer to Phase 6+ |
| iOS at launch | $99/yr. Android + WebGL only for v1 |

---

## 12. What we do that Last War does **not** — your competitive edge

These are your two features, and they are genuinely good ideas because they fill real gaps:

### ★ 1. Alliance voice chat
**The gap:** Last War alliances coordinate timed, high-stakes events (Desert Storm, rallies, Capitol Clash) through **typed text, across language barriers, in real time.** It is genuinely painful. Serious alliances migrate to Discord, which means the game **leaks its own social graph to a third party.**

**The edge:** in-game voice keeps the social graph inside your game. Players whose friendships live in your app do not churn. This is a retention feature disguised as a convenience feature.

Full spec: `10-FEATURE-voice-chat.md`

### ★ 2. Arena rooms
**The gap A:** in Last War you **cannot fight your own alliance members**. There is no sparring, no internal ranking, no way to test a formation safely. Alliance mates are permanent allies you never interact with combatively — a huge missed engagement opportunity.

**The gap B:** alliance-vs-alliance combat is limited to slow, scheduled, asynchronous march-based events. There is no fast, direct, "let's fight right now" outlet.

**The edge:**
- **Intra-alliance sparring** with zero losses gives every player a **risk-free, skill-based, repeatable** activity — and F2P players in particular finally get something to do that isn't gated by wallet (especially with power normalization on)
- **AvA arena rooms** provide instant, scheduled-or-on-demand alliance war that is *fun to watch and talk over* — which makes voice chat immediately valuable, so the two features reinforce each other

Full spec: `11-FEATURE-arena-rooms.md`

---

## 13. Distilled design laws (the takeaways that matter)

1. **The front-end hook acquires; the back-end depth retains.** You need both, but you must build the hook first because without players nothing else matters.
2. **You are selling time, not resources.** Every system should create a wait that money can shorten.
3. **Make alliances mechanically mandatory, not socially optional.** Alliance Help is how.
4. **Never let a player lose what they built.** Wounded-not-dead, warehouse protection, shields.
5. **Convert individual spending into social benefit.** Alliance Gifts turn whales into local heroes.
6. **Scheduled events beat available content.** Appointment mechanics create habit; habit creates revenue.
7. **The endgame content is other players.** Server politics are free to produce and infinitely renewable.
8. **If a change needs an app update, live-ops is dead.** Data-driven everything, server-driven UI.
9. **The stall point is the funnel.** Design difficulty walls deliberately and place them honestly.
10. **Retain the non-payers.** They are the content your payers are buying access to.

---

## Next

- `02-vision-scope-ladder.md` — what Zero Hour actually is, and what "done" means at each stage
- `06-GDD-runner-minigame.md` — Phase 1, the first thing we build
- `10` / `11` — your two new features, fully specified
