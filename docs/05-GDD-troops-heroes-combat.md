# 05 — GDD: Troops, Heroes & Combat

> Troops are the economy's output. Heroes are the monetization. Combat is where both are spent.
> Everything in this document is deterministic and lives in `shared/Sim`.

---

## 1. Troop types & the counter triangle

Three types. Rock-paper-scissors.

```
        MISSILE
        ▲     ╲
       ╱        ▼
    AIR  ◀────  TANK
```

| Type | Beats | Loses to | Character |
|---|---|---|---|
| **Tank** | Air | Missile | High HP, low damage, front line |
| **Air** | Missile | Tank | High damage, low HP, fast |
| **Missile** | Tank | Air | Long range, high burst, fragile |

```
CounterMultiplier:
  advantage  = 1.30
  neutral    = 1.00
  disadvantage = 0.77     (≈ 1/1.30, so the triangle is symmetric)
```

Stored in `tools/balance/counter_triangle.csv` and hot-reloadable. **Do not hardcode this.** It will be retuned repeatedly.

> **Note:** the orientation above is our design choice. The reference game's exact orientation is disputed in community sources and is irrelevant — what matters is that the triangle is symmetric, readable, and tunable.

### Why three types and not more
Each added type expands the balance surface quadratically. Three is the minimum for a meaningful triangle and the maximum a mobile player will reason about while also managing an economy. See non-goals in `02`.

---

## 2. Troop tiers

**8 tiers per type** (T1–T8). Unlocked by HQ level and troop tech.

| Tier | HQ unlock | Power index | Train time | Notes |
|---|---|---|---|---|
| T1 | 3 | 1.0 | 4 s | |
| T2 | 6 | 1.9 | 9 s | |
| T3 | 9 | 3.5 | 20 s | |
| T4 | 12 | 6.5 | 45 s | |
| T5 | 15 | 12 | 100 s | |
| T6 | 19 | 22 | 210 s | |
| T7 | 23 | 40 | 440 s | |
| T8 | 27 | 73 | 900 s | endgame |

```
TroopStat(tier)   = base · 1.85^(tier-1)
TrainTime(tier)   = 4s · 2.2^(tier-1)
TrainCost(tier)   = baseCost · 2.1^(tier-1)
```

### Promotion, not replacement
Lower-tier troops **promote** to higher tiers at a discount rather than being made obsolete.

```
PromoteCost(from→to) = TrainCost(to) - TrainCost(from) · 0.7
PromoteTime          = TrainTime(to) · 0.5
```

**Why this matters:** a player who spent three weeks building a T4 army must never open the game to discover it is worthless. Promotion means all past investment carries forward. This is a retention mechanic disguised as an economy mechanic.

### Troop capacity
```
MaxTroops = 500 + HQLevel · 400 + techBonus + vipBonus
```
Capacity is a soft cap — exceeding it costs extra food upkeep, it doesn't hard-block.

---

## 3. Heroes

### 3.1 Roster at launch: 18 heroes

| Rarity | Count | Pull rate | Role |
|---|---|---|---|
| **R** (Rare) | 6 | 74.5% | Starter heroes, always useful, never great |
| **SR** (Super Rare) | 8 | 23% | The workhorse tier |
| **UR** (Ultra Rare) | 4 | 2.5% | Meta-defining, the gacha target |

Six heroes per troop type (2R / 3SR / 1UR each), so every type has a viable path at every rarity. **No troop type may be strictly weaker at any rarity tier** — that would collapse the triangle.

### 3.2 Progression axes (3 at launch)

| Axis | Currency | Ceiling | Effect |
|---|---|---|---|
| **Level** | Hero EXP | 100 | Linear stat growth |
| **Stars** | Duplicate shards | 6★ | Large stat jumps + skill unlocks |
| **Skills** | Skill Books | 4 skills × L10 | Active/passive combat effects |

Deferred to Phase 6+ as the content pipeline: **Exclusive Gear**, **Awakening**.

```
HeroStat(level, star) = base · (1 + 0.035·level) · starMult[star]
starMult = [1.0, 1.25, 1.55, 1.95, 2.45, 3.10, 4.00]
```

### 3.3 Skills
Each hero has 4 skills unlocked at 1★/2★/3★/4★:
1. **Active** — triggers on a cooldown in battle
2. **Passive (combat)** — flat/% stat bonus in battle
3. **Passive (economy)** — bonus when the hero is assigned to a job (gathering, construction, training)
4. **Leader** — only active when the hero leads the squad

The economy skill matters more than it looks: it gives heroes value outside combat, which means every hero has a use and no pull feels wasted.

### 3.4 Squads
- A squad = **3 heroes** + assigned troops
- The **leader** (slot 1) determines the squad's troop type and applies its Leader skill
- Squad count: 2 at HQ 5, +1 at HQ 10, 15, 20, 25 (max 6)
- Each squad has independent troop assignment and march capacity

```
SquadPower = Σ heroPower + Σ(troopCount · troopPower) · (1 + squadBonuses)
```

---

## 4. Gacha

### 4.1 Banners
| Banner | Cost | Contents |
|---|---|---|
| **Standard** | 1 Recruit Ticket / 150💎 | Full pool |
| **Featured** | 1 Premium Ticket / 300💎 | Rate-up on one UR |
| **Beginner** | Free ×10 (first 7 days) | Guaranteed 1 SR+ |

10-pull gives a **10% discount** and a **guaranteed SR or better**.

### 4.2 Pity — published, hard, honest

```
Soft pity : from pull 45, UR rate increases by +4% per pull
Hard pity : pull 60 guarantees a UR                (counter resets)
Featured  : 50% chance the guaranteed UR is the featured hero.
            If not, the NEXT guaranteed UR IS the featured hero.
```

That last clause is the "guaranteed within two pity cycles" rule. It means the worst case is knowable, which converts gambling anxiety into a purchase decision. It is both more ethical and more effective.

### 4.3 Duplicates
Duplicates always convert to shards. **There is no dead pull.**

| Rarity | Shards from dupe | Shards to +1★ |
|---|---|---|
| R | 20 | 40 |
| SR | 15 | 80 |
| UR | 10 | 120 |

### 4.4 Published rates
Rates are shown **in the recruitment UI itself**, not buried in a legal page. This is:
- Legally required in several territories (see `22`)
- A trust signal a small studio badly needs
- Statistically verified by a test that runs 100,000 simulated pulls in CI

---

## 5. Combat model

### 5.1 Two combat systems (do not confuse them)

| System | Where | How | Doc |
|---|---|---|---|
| **Resolved combat** | World map PvP, zombies, rallies | Server computes the whole battle instantly, returns a report + replay data | this doc |
| **★ Real-time combat** | Arena rooms | Server ticks at 20 Hz, players issue orders live | `11-FEATURE-arena-rooms.md` |

Both use the **same stat model and the same `shared/Sim` damage formulas**. Only the time model differs. This is deliberate — one source of truth for balance.

### 5.2 Resolved combat algorithm

```
Input:  attackerSquads[], defenderSquads[], defenderBase?, seed
Output: BattleReport { winner, roundLog[], losses, loot, seed }

1. Compute effective stats for each squad:
     Atk = Σ(troopCount · troopAtk(tier))
         · (1 + heroAtk% + techAtk% + vipAtk% + allianceTechAtk%)
     Def, HP  analogous
     Apply counterMultiplier(attackerType, defenderType)
     Apply terrainMultiplier (world map tile)
     Apply defenderBaseBonus (wall + turrets), defense only

2. Round loop (max 30 rounds):
     a. Trigger any hero actives whose cooldown is ready (deterministic order)
     b. damage = Atk² / (Atk + Def) · variance(seed, round)
        variance ∈ [0.95, 1.05]  — small, deterministic, from seed
     c. Apply damage to HP pools; convert HP loss to troop losses
     d. Break when one side's HP ≤ 0 or round 30 (defender wins ties)

3. Split losses into wounded / dead (see 04 §9)
4. Compute loot (see 04 §4)
5. Emit BattleReport
```

### 5.3 Determinism requirements — non-negotiable

`shared/Sim` combat code obeys:

| Rule | Why |
|---|---|
| **No `float` or `double`** — fixed-point `Q32.32` only | IEEE754 rounding differs across CPUs/platforms; ARM vs x86 will diverge |
| **No `UnityEngine` types** | The server has no Unity |
| **No `DateTime.Now`** | Time is an explicit parameter |
| **No unordered iteration** — no `Dictionary` enumeration in sim paths | Hash order varies across runtimes |
| **Seeded PRNG only** (xoshiro256\*\*, our own implementation) | `System.Random` differs across .NET versions |
| **No LINQ in hot/sim paths** | Allocation + ordering risk |

**Verification (Phase 3 gate):** 1,000 randomised battle fixtures produce byte-identical `BattleReport` hashes on the Unity client (Android ARM64), the Unity editor (Windows x64), and the ASP.NET server. Automated in CI.

### 5.4 Battle reports & replays
```
BattleReport = { seed, participantSnapshot, roundLog[], outcome }
```
- Stored server-side, ~2–4 KB
- Client renders an animated replay from the round log
- Shareable to alliance chat
- **Doubles as an anti-cheat audit trail** — any disputed result can be re-simulated server-side from the seed

---

## 6. Power score

One number summarising account strength. Used for matchmaking, leaderboards, and ★ arena brackets.

```
Power = buildingPower + techPower + troopPower + heroPower

buildingPower = Σ level² · buildingWeight
techPower     = Σ nodeLevel · nodeWeight
troopPower    = Σ count · tierPowerIndex
heroPower     = Σ (level · 10 + starMult · 500 + Σ skillLevel · 25)
```

**Warning:** power score is a *bad* proxy for actual combat strength (composition and counters matter more), and players will trust it anyway. Two mitigations:
1. ★ Arena matchmaking uses power **as a bracket**, not an exact match
2. ★ Power-normalized sparring exists specifically so skill can beat power

---

## 7. Stat & buff stacking rules

Ambiguity here causes balance chaos. The rules are explicit:

```
FinalStat = BaseStat
          · (1 + Σ additivePercents)     ← tech, VIP, alliance tech, hero passives
          · Π (1 + multiplicativePercent) ← event buffs, titles, ★ arena modifiers
          · counterMultiplier
          · terrainMultiplier
```

- **Additive by default.** Tech, VIP, alliance tech, and hero passives all sum before applying. This keeps them from exploding.
- **Multiplicative only for temporary buffs** (events, President titles, arena modifiers), which are rare and time-boxed.
- **Hard cap:** no single stat may exceed **5×** its base value from all permanent sources. Checked and clamped in `Sim`. This is the guardrail that prevents late-game power inflation from breaking every other system.

---

## 8. Hero jobs (economy skills)

Heroes not in a squad can be assigned to a job, using their economy passive:

| Job | Effect |
|---|---|
| Construction | −% build time |
| Research | −% research time |
| Training | −% train time |
| Gathering | +% node yield |

3 job slots, unlocked at HQ 7 / 12 / 18. This gives bench heroes value and creates a real decision about roster depth versus squad quality.

---

## 9. Balance data files

| File | Contents |
|---|---|
| `troops.csv` | type, tier, hqUnlock, atk, def, hp, trainSeconds, cost, powerIndex |
| `counter_triangle.csv` | attackerType, defenderType, multiplier |
| `heroes.csv` | id, name, rarity, troopType, baseAtk, baseDef, baseHp |
| `hero_skills.csv` | heroId, slot, type, unlockStar, effectId, scaling |
| `hero_stars.csv` | star, shardsRequired, statMultiplier |
| `gacha_banners.csv` | bannerId, cost, poolId, rateUpHeroId, softPityStart, hardPity |
| `gacha_rates.csv` | poolId, rarity, rate |
| `combat_constants.csv` | variance range, max rounds, wound rate, stat caps |

---

## 10. Anti-frustration guarantees

| Guarantee | Mechanism |
|---|---|
| A pull is never wasted | Dupes → shards, always |
| The worst-case gacha outcome is knowable | Hard pity 60, published |
| An army is never obsolete | Promotion carries value forward |
| A defeat never destroys you | Wounded rule, 70–90% recovered |
| Every hero has a use | Economy passives + job slots |
| Power can be beaten by play | ★ Power-normalized arena mode |
| Every troop type is viable | Symmetric triangle, equal rarity distribution |

---

## Next
- `06-GDD-runner-minigame.md` — Phase 1, what we build first
- `11-FEATURE-arena-rooms.md` — real-time combat using this same stat model
- `12-economy-balance-model.md` — how all of this is tuned together
