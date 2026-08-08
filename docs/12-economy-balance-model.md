# 12 — Economy & Balance Model

> Every number in the game in one place, with the reasoning behind it.
> All values live in `tools/balance/*.csv`. **Nothing here is hardcoded in C#.**

---

## 1. Principles

1. **Sell time, not power.** Every purchase shortens a wait or skips a grind. Nothing is purchase-exclusive.
2. **Every wall is passable free.** Slower, but passable. If a wall requires payment, it's a bug.
3. **Sinks must exceed faucets** at every progression stage, or the economy inflates and timers stop mattering.
4. **Geometric costs, geometric rewards.** Both sides scale at similar rates so relative progress feels steady.
5. **One source of truth.** `shared/Sim` computes all costs and rewards; client and server cannot disagree.

---

## 2. Currency map

| Currency | Faucets | Sinks | Inflation risk |
|---|---|---|---|
| **Food** | Farms, nodes, runner, events, offline | Building, training, healing | High → capped storage, overflow wasted |
| **Iron** | Mines, nodes, runner, events, offline | Building, research | High → same |
| **Coin** | Bank, events, missions | Research, hero levels | Medium |
| **Diamonds** 💎 | IAP, missions, events, achievements | Speedups, gacha, shields, refills | Low (premium) |
| **Speedups** | Events, missions, IAP, alliance shop | Time reduction | Medium → the real currency of the endgame |
| **Valor** | Alliance help, donations, ★ arena | Alliance shop | Medium → weekly stock rotation is the sink |

### The overflow rule
Resource storage is capped at **8 hours of production**. Overflow is **wasted, not banked.**

This is the load-bearing anti-inflation mechanism. Without it, a player who leaves for a week returns to a full economy and the whole timer structure collapses. With it, they return to a full-but-capped store and a reason to check in more often.

---

## 3. Core formulas

```
# Buildings
BuildTime(L)      = 20s · 1.42^L                     capped 4 days
BuildCost(L)      = baseCost · 1.55^L
Production(L)     = base · L^1.35 · (1 + bonuses)
Capacity(L)       = Production(L) · 8
Protected(L)      = wpBase · L^1.5

# Research
ResearchTime(n,L) = nodeBase · 1.5^L                 capped 4 days
ResearchCost(n,L) = nodeCost · 1.6^L

# Troops
TroopStat(T)      = base · 1.85^(T-1)
TrainTime(T)      = 4s · 2.2^(T-1)
TrainCost(T)      = baseCost · 2.1^(T-1)
PromoteCost(a→b)  = TrainCost(b) - TrainCost(a)·0.7

# Heroes
HeroStat(lvl,★)   = base · (1 + 0.035·lvl) · starMult[★]
HeroExpToLevel(l) = 50 · 1.09^l

# Runner
BossHP(s)         = 100 · 1.155^s
IdleRate(s)       = baseRate · 1.14^s · (1 + idleBonus)

# Power
Power = Σ buildingL² · w + Σ techL · w + Σ troops · tierIdx + Σ heroPower
```

---

## 4. Progression targets

The pacing contract. If live data diverges from this, retune the CSVs, not the code.

| Milestone | F2P time | Light spender (~$20) | Whale (~$500+) |
|---|---|---|---|
| HQ 5 (world map) | 25 min | 15 min | 8 min |
| HQ 10 (arms race) | 3 h | 1.5 h | 30 min |
| HQ 15 | 3 days | 1.5 days | 6 h |
| HQ 20 (capitol) | 3 weeks | 12 days | 2 days |
| HQ 25 | 8 weeks | 5 weeks | 1 week |
| HQ 30 (endgame) | 5 months | 3 months | 3 weeks |

**The whale-to-F2P ratio is roughly 6:1 in time.** That is the design target. Much wider and F2P players quit because the gap is hopeless; much narrower and whales stop spending because money buys nothing.

---

## 5. Faucet/sink budget (per day, HQ 15 player) ⚠️

| Source | Food/day |
|---|---|
| Farm production | 180k |
| Node gathering (4 marches) | 240k |
| Runner + idle | 60k |
| Events + missions | 120k |
| **Total faucet** | **600k** |

| Sink | Food/day |
|---|---|
| Building upgrades | 300k |
| Troop training | 200k |
| Healing | 60k |
| Research | 80k |
| **Total sink** | **640k** |

**Sink/faucet ≈ 1.07.** Slightly sink-heavy on purpose — the player is always mildly short, which is what creates the desire to gather more, join events, and occasionally buy. If this ratio drops below 1.0, resources pile up and timers become the only constraint, which makes the game feel like a waiting room.

---

## 6. Diamond value anchoring

```
1 Speedup-hour   ≈ 12 💎
1 Gacha pull     = 150 💎  (300 featured)
1 24h Shield     = 200 💎
1 Stamina refill = 50 💎
Instant finish   = 12 💎 per remaining hour, min 5
```

Reference: **$4.99 ≈ 500 💎** (plus bonuses on larger packs). So one pull ≈ $1.50, one shield-day ≈ $2. Compare against genre norms during tuning; being modestly more generous than average is a deliberate positioning choice for a small studio without a UA budget — word of mouth is our acquisition channel.

---

## 7. Balance validation (automated, CI)

Tests that must pass on every change to `tools/balance/*`:

| Test | Assertion |
|---|---|
| **Monotonic costs** | Cost and time strictly increase with level for every table |
| **No negative or zero costs** | Anywhere |
| **Reachability** | A simulated F2P player reaches HQ 30 within 6 months of simulated play |
| **No paywalls** | Every unlock is reachable with zero spend |
| **Sink/faucet ratio** | Between 1.0 and 1.2 at HQ 5/10/15/20/25/30 |
| **Gacha rates** | 100k simulated pulls land within ±0.3% of published rates |
| **Pity** | Hard pity always fires by pull 60; the two-cycle featured guarantee always holds |
| **Stat cap** | No permanent stat combination exceeds 5× base |
| **Counter symmetry** | `advantage · disadvantage ≈ 1.0` |
| **★ Arena normalization** | Normalized power spread across HQ 8–30 stays within ±10% |
| **★ Sparring is lossless** | Simulated sparring changes no resource or troop count |

The economy simulator (`tools/balance/simulate.py`) plays a virtual F2P player, a light spender, and a whale through 180 simulated days and reports the milestone table in §4. Run it after every balance change.

---

## 8. Live tuning process

1. Read the metrics (`21-analytics-kpis.md`)
2. Identify the stall point (where progression rate drops off a cliff)
3. Change the CSV
4. Run the validation suite + simulator
5. Hot-reload on the server (no client build)
6. Watch for 48 h
7. Roll back if retention drops

**Rule: change one thing at a time.** Two simultaneous balance changes produce uninterpretable data, and you will end up guessing.

---

## Next
- `13-monetization-iap.md`
- `21-analytics-kpis.md`
